using api_be.Application.Models.Request;
using api_be.Application.Models.ValidatorRequest.DefaultValidatorBase;
using api_be.Application.Models.ValidatorRequest.OrderValidator.BaseOrders;
using api_be.Application.Responses;
using api_be.Core.Constants;
using api_be.Core.Domain.Interfaces;
using api_be.Core.Entities;
using api_be.Domain.Constants;
using api_be.Domain.Exceptions;
using api_be.Domain.Extensions;
using api_be.Domain.ResultResponses;
using api_be.Infrastructure.DB;
using api_be.Middleware;
using AutoMapper;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Core.Bulk;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sieve.Models;
using Sieve.Services;
using System.Linq;
using System.Text.Json;

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
        private readonly ILogger<ProductElasticService> _logger;


        public ProductElasticService(
            ElasticsearchClient elasticClient,
            ISupermarketDbContext context,
            IMapper mapper,
            ICurrentUserService currentUserService,
             ILogger<ProductElasticService> logger,
            ISieveProcessor sieveProcessor)
        {
            _elasticClient = elasticClient;
            _context = context;
            _mapper = mapper;
            _currentUserService = currentUserService;
            _sieveProcessor = sieveProcessor;
            _logger = logger;

        }

        // Index name for Elasticsearch
        private const string PRODUCT_INDEX = ELASTIC_SEARCH_VALUE.PRODUCT_INDEX;
        public async Task<IQueryable<Product>> IndexProductsAsync()
        {
            var products = await GetListProductAsync();
            var productList = await products.Include(p => p.Category).ToListAsync();

            foreach (var product in productList)
            {
                var productForIndex = new
                {
                    id = product.Id,
                    internalCode = product.InternalCode,
                    name = product.Name,
                    images = product.Images?.Split(',').Select(s => s.Trim()).ToList() ?? new List<string>(),
                    price = product.Price ?? 0,
                    quantity = product.Quantity ?? 0,
                    describes = product.Describes,
                    feature = product.Feature,
                    specifications = product.Specifications,
                    type = (int?)product.Type,        // ← INTEGER
                    status = (int?)product.Status,    // ← INTEGER
                    selling = product.Selling,
                    parentId = product.ParentId,
                    categoryId = product.CategoryId,
                    category = product.Category != null ? new
                    {
                        id = product.Category.Id,
                        internalCode = product.Category.InternalCode,
                        name = product.Category.Name,
                        icon = product.Category.Icon,
                        parentId = product.Category.ParentId
                    } : null,
                    createdAt = product.CreatedAt,
                    updatedAt = product.UpdatedAt,
                    createdBy = product.CreatedBy,
                    updatedBy = product.UpdatedBy,
                    isDeleted = product.IsDeleted
                };

                await _elasticClient.IndexAsync(productForIndex, idx => idx.Index(PRODUCT_INDEX));
            }


            return products;
        }
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

                var deleteRequest = new DeleteByQueryRequest(PRODUCT_INDEX)
                {
                    Query = new MatchAllQuery()
                };
                await _elasticClient.DeleteByQueryAsync(deleteRequest);

                var bulkRequest = new BulkRequest(PRODUCT_INDEX)
                {
                    Operations = new BulkOperationsCollection(
                        query.Select(p => new BulkIndexOperation<Product>(p)).ToList()
                    )
                };
                await _elasticClient.BulkAsync(bulkRequest);

            //    var productsRs = await query
            //.Where(p => p.Name.ToLower().Contains(request.SearchKeyword.ToLower()))
            //.Select(p => _mapper.Map<ProductDto>(p))
            //.ToListAsync();

                     var keywordPattern = $"%{request.SearchKeyword}%";

                var productsRs = await query
                    .Include(p => p.Category)
                    .Where(p => EF.Functions.Like(p.Name, keywordPattern)
                             || EF.Functions.Like(p.Category.Name, keywordPattern)
                             || EF.Functions.Like(p.Feature, keywordPattern)
                             || EF.Functions.Like(p.Specifications, keywordPattern))
                    .Select(p => _mapper.Map<ProductDto>(p))
                    .ToListAsync();


                if (productsRs.Any())
                {
                    throw new NotFoundException("No matching product found.");

                }

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


        public async Task<List<ProductDto>> SearchProductsAsync(string searchTerm, int page = 1, int pageSize = 10)
        {
            var response = await _elasticClient.SearchAsync<Product>(s => s
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

            return _mapper.Map<List<ProductDto>>( response.Documents.ToList());
        }

        public async Task<List<ProductDto>> SearchDetailedProductsAsync(string searchTerm)
        {
            var response = await _elasticClient.SearchAsync<dynamic>(s => s
                .Index(PRODUCT_INDEX)
                .Query(q => q
                    .Bool(b => b
                        .Should(
                            bs => bs.Match(m => m
                                .Field("name")
                                .Query(searchTerm)
                                .Fuzziness(new Fuzziness(1))
                            ),
                            bs => bs.Match(m => m
                                .Field("category.name")
                                .Query(searchTerm)
                            ),
                            bs => bs.MatchPhrasePrefix(m => m
                                .Field("feature")
                                .Query(searchTerm)
                            ),
                            bs => bs.MatchPhrasePrefix(m => m
                                .Field("specifications")
                                .Query(searchTerm)
                            ),
                            bs => bs.MatchPhrasePrefix(m => m
                                .Field("describes")
                                .Query(searchTerm)
                            )
                        )
                        .MinimumShouldMatch(1)
                    )
                )
                .Size(100)
            );

            if (!response.IsValidResponse)
            {
                _logger.LogError($"Elasticsearch search failed: {response.DebugInformation}");
                return new List<ProductDto>();
            }

            var productDtos = new List<ProductDto>();

            foreach (var hit in response.Documents)
            {
                try
                {
                    // Parse dynamic object
                    var product = new Product
                    {
                        Id = Convert.ToInt32(hit.id),
                        InternalCode = hit.internalCode,
                        Name = hit.name,
                        Images = hit.images != null ? string.Join(",", hit.images) : null,
                        Price = hit.price != null ? Convert.ToDecimal(hit.price) : null,
                        Quantity = hit.quantity != null ? Convert.ToInt32(hit.quantity) : null,
                        Describes = hit.describes,
                        Feature = hit.feature,
                        Specifications = hit.specifications,

                        // Parse INTEGER to ENUM
                        Type = hit.type != null
                            ? (Product.ProductType)Convert.ToInt32(hit.type)
                            : (Product.ProductType?)null,
                        Status = hit.status != null
                            ? (Product.ProductStatus)Convert.ToInt32(hit.status)
                            : (Product.ProductStatus?)null,

                        Selling = hit.selling != null ? Convert.ToInt32(hit.selling) : null,
                        ParentId = hit.parentId != null ? Convert.ToInt32(hit.parentId) : null,
                        CategoryId = hit.categoryId != null ? Convert.ToInt32(hit.categoryId) : null,
                        CreatedAt = hit.createdAt != null ? DateTime.Parse(hit.createdAt.ToString()) : null,
                        UpdatedAt = hit.updatedAt != null ? DateTime.Parse(hit.updatedAt.ToString()) : null,
                        CreatedBy = hit.createdBy,
                        UpdatedBy = hit.updatedBy,
                        IsDeleted = hit.isDeleted != null ? Convert.ToBoolean(hit.isDeleted) : false
                    };

                    // Parse category
                    if (hit.category != null)
                    {
                        product.Category = new Category
                        {
                            Id = Convert.ToInt32(hit.category.id),
                            InternalCode = hit.category.internalCode,
                            Name = hit.category.name,
                            Icon = hit.category.icon,
                            ParentId = hit.category.parentId != null ? Convert.ToInt32(hit.category.parentId) : null
                        };
                    }

                    var productDto = _mapper.Map<ProductDto>(product);

                    // Apply promotion
                    (Promotion promo, decimal? discountPrice, int? group) =
                        await BaseOrderApplyPromotion.ApplyPromotionForSingleProduct(_context, product);

                    productDto.NewPrice = product.Price - discountPrice;
                    productDto.PromotionDto = _mapper.Map<PromotionDto>(promo);

                    productDtos.Add(productDto);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error parsing product from Elasticsearch hit");
                }
            }

            return productDtos;
        }
    }


}