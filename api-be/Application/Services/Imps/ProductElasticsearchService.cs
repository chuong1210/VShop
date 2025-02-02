using api_be.Domain.Constants;
using api_be.Core.Domain.Interfaces;
using api_be.Core.Entities;
using api_be.Domain.Models.Responses;
using api_be.Application.ValidatorRequest.OrderValidator.BaseOrders;
using api_be.Domain.DefaultValidatorBase;
using api_be.Infrastructure.DB;
using System.Text.Json;
using Elastic.Clients.Elasticsearch;
using AutoMapper;
using Elastic.Clients.Elasticsearch.Core.Bulk;
using Elastic.Clients.Elasticsearch.QueryDsl;
using api_be.Domain.Models.Request;
using Microsoft.AspNetCore.Http;
using Sieve.Models;
using api_be.Domain.Extensions;
using Microsoft.EntityFrameworkCore;
using Sieve.Services;
using System.Linq;
using api_be.Core.Constants;
using api_be.Middleware;
using Microsoft.Extensions.DependencyInjection;

namespace api_be.Application.Services.Imps
{
    [RegisterService(ServiceLifetime.Scoped)]

    public class ProductElasticService:IProductElasticsearchService
    {
        private readonly ElasticsearchClient _elasticClient;
        private readonly ISupermarketDbContext _context;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;
        private readonly ISieveProcessor _sieveProcessor;

        public ProductElasticService(
            ElasticsearchClient elasticClient,
            ISupermarketDbContext context,
            IMapper mapper,
            ICurrentUserService currentUserService,
            ISieveProcessor sieveProcessor)
        {
            _elasticClient = elasticClient;
            _context = context;
            _mapper = mapper;
            _currentUserService = currentUserService;
            _sieveProcessor = sieveProcessor;
        }

        // Index name for Elasticsearch
        private const string PRODUCT_INDEX = ELASTIC_SEARCH_VALUE.PRODUCT_INDEX;
        public async Task<IQueryable<Product>> GetListProductAsync()
        {
       


                var query = _context.Set<Product>().FilterDeleted();

           
                if (_currentUserService.Type != CLAIMS_VALUES.TYPE_ADMIN &&
           _currentUserService.Type != CLAIMS_VALUES.TYPE_SUPER_ADMIN)
                {
                    query = query.Where(x => x.Status == Product.ProductStatus.Active);
                }

      
                query = query.Where(x => x.Type == Product.ProductType.Option);

        
      
   


            return query;
        }



        public async Task<PaginatedResult<List<ProductDto>>> GetListSearchProduct(ListBaseCommand request)
        {
            var results = await SearchDetailedProductsAsync(request.SearchKeyword);
            if (!results.Any())
            {
                var query = await GetListProductAsync();
         

                var deleteRequest = new DeleteByQueryRequest("products")
                {
                    Query = new MatchAllQuery()
                };
                await _elasticClient.DeleteByQueryAsync(deleteRequest);

                var bulkRequest = new BulkRequest("products")
                {
                    Operations = new BulkOperationsCollection(
                        query.Select(p => new BulkIndexOperation<Product>(p)).ToList()
                    )
                };
                await _elasticClient.BulkAsync(bulkRequest);

                var productsRs = query
                         .Where(p => p.Name.Contains(request.SearchKeyword, StringComparison.OrdinalIgnoreCase))
                         .Select(p => _mapper.Map<ProductDto>(p))
                         .ToList();
                var sieveModel = _mapper.Map<SieveModel>(request);

                int totalCount = await PaginatedResultBase.CountApplySieveAsync(_sieveProcessor, sieveModel, query);


                var filteredQuery = _sieveProcessor.Apply(sieveModel, query);




                for (int i = 0; i < productsRs.Count; i++)
                {
                    var product = _mapper.Map<Product>(productsRs[i]);

                    (Promotion promo, decimal? priceDiscoutMax, int? group) =
                            await BaseOrderApplyPromotion.
                                ApplyPromotionForSingleProduct(_context, product);

                    productsRs[i].NewPrice = product.Price - priceDiscoutMax;
                    productsRs[i].PromotionDto = _mapper.Map<PromotionDto>(promo);
                }

                return PaginatedResult<List<ProductDto>>.Create(productsRs, totalCount, request.Page.Value, request.PageSize.Value, StatusCodes.Status200OK);
            }
            return PaginatedResult<List<ProductDto>>.Create(results, results.Count, request.Page.Value, request.PageSize.Value, StatusCodes.Status200OK);
        }
        public async Task<List<ProductDto>> SearchDetailedProductsAsync(string searchTerm, int page = 1, int pageSize = 10)
        {
            var response = await _elasticClient.SearchAsync<ProductDto>(s => s
                .Index(PRODUCT_INDEX)
                .Query(q => q
                    .Bool(b => b
                        .Should(
                            bs => bs.MultiMatch(m => m
                                .Fields(new[] { "name", "category.name", "describes", "feature", "specifications" })
                                .Query(searchTerm)
                            )
                        )
                    )
                )
                .From((page - 1) * pageSize)
                .Size(pageSize)
            );

            if (!response.IsValidResponse || response.Documents == null)
            {
                return new List<ProductDto>();
            }

            return response.Documents.ToList();
        }

        public async Task<List<ProductDto>> SearchDetailedProductsAsync(string searchTerm)
        {
            var response = await _elasticClient.SearchAsync<ProductDto>(s => s
                .Index("products")
                .Query(q => q
                    .Bool(b => b
                        .Should(
                            bs => bs.Match(m => m.Field(f => f.Name).Query(searchTerm)),
                            bs => bs.Match(m => m.Field(f => f.Category.Name).Query(searchTerm)),
                            bs => bs.Match(m => m.Field(f => f.Feature).Query(searchTerm)),
                            bs => bs.Match(m => m.Field(f => f.Specifications).Query(searchTerm))
                        )
                    )
                )
            );

            if (!response.IsValidResponse)
            {
                return new List<ProductDto>();
            }

            var products = response.Documents.ToList();

            // Lấy thêm thông tin khuyến mãi và tính giá mới nếu có
            foreach (var product in products)
            {
                (Promotion promo, decimal? discountPrice, int? group) =
                    await BaseOrderApplyPromotion.ApplyPromotionForSingleProduct(_context,_mapper.Map<Product>(product));

                product.NewPrice = product.Price - discountPrice;
                product.PromotionDto = _mapper.Map<PromotionDto>(promo);
            }

            return products;
        }
    }


}