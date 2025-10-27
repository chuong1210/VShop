using api_be.Domain.Constants;
using api_be.Infrastructure.Services;
using api_be.Core.Entities;
using api_be.Core.Domain.Interfaces;
using api_be.Domain.Exceptions;
using api_be.Domain.Extensions;
using api_be.Application.Models.Request;
using api_be.Application.Responses;
using api_be.Application.Models.ValidatorRequest.DefaultValidatorBase;
using api_be.Domain.Transforms;
using api_be.Application.Models.ValidatorRequest.BaseCategory;
using api_be.Application.Models.ValidatorRequest.BaseProduct;
using AutoMapper;
using CloudinaryDotNet;
using Microsoft.EntityFrameworkCore;
using Sieve.Models;
using Sieve.Services;
using System.Security.Principal;
using System.Threading;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using static api_be.Core.Entities.Promotion;
using static api_be.Domain.Extensions.ValidatorExtension;
using api_be.Middleware;
using static api_be.Core.Entities.Product;
using static System.Net.Mime.MediaTypeNames;
using api_be.Application.Models.ValidatorRequest;
using System;
using api_be.Infrastructure.DB;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using api_be.Domain.ResultResponses;
using api_be.Application.Models.ValidatorRequest.OrderValidator.BaseOrders;
using Newtonsoft.Json;

namespace api_be.Application.Services.Imps
{
    [RegisterService(ServiceLifetime.Scoped)]

    public class ProductService : IProductService
    {
        private readonly ISupermarketDbContext _context;
        private readonly IMapper _mapper;
        private readonly ISieveProcessor _sieveProcessor;
        private readonly Cloudinary _cloudinary;
        private readonly ILogger<ProductService> _logger;
        private readonly ICurrentUserService _currentUserService;
        private readonly IRedisInventoryService _redisInventoryService;
        private readonly IRedisCacheService _redisCacheService; // Inject RedisCacheService
        private readonly TimeSpan _cacheExpiry = TimeSpan.FromHours(1);



        public ProductService(ISupermarketDbContext context, IMapper mapper, ISieveProcessor sieveProcessor, Cloudinary cloudinary, ILogger<ProductService> logger, 
            ICurrentUserService currentUserService, IRedisInventoryService redisInventoryService, IRedisCacheService redisCacheService)
        {
            _context = context;
            _mapper = mapper;
            _sieveProcessor = sieveProcessor;
            _cloudinary = cloudinary;
            _logger = logger;
            _currentUserService = currentUserService;
            _redisInventoryService = redisInventoryService;
            _redisCacheService = redisCacheService;
        }
        public async Task SyncInventoryToRedisAsync()
        {
            try
            {
                var products = await _context.Products.ToListAsync(); // Lấy tất cả sản phẩm từ database

                foreach (var product in products)
                {
                    // Lưu số lượng tồn kho của từng sản phẩm vào Redis
                    bool success = await _redisInventoryService.SetStockLevelAsync(product.Id, product.Quantity ?? 0);

                    if (!success)
                    {
                        _logger.LogError("Failed to sync inventory for product {ProductId} to Redis", product.Id);
                    }
                }

                _logger.LogInformation("Inventory synchronization to Redis completed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during inventory synchronization to Redis");
            }
        }
        public async Task<Result<bool>> ChangeStatus(ChangeStatusProductRequest request)
        {
            try
            {

                var validator = new ChangeStatusProductValidator(_context);
                var validationResult = await validator.ValidateAsync(request);

                if (validationResult.IsValid == false)
                {
                    var errorMessages = validationResult.Errors.Select(x => x.ErrorMessage).ToList();
                    return Result<bool>.Failure(errorMessages, StatusCodes.Status400BadRequest);
                }

                var product = await _context.Products.FindAsync(request.ProductId);

                product.Status = request.Status;

                _context.Products.Update(product);
                await _context.SaveChangesAsync();

                return Result<bool>.Success(true, StatusCodes.Status200OK);
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure(ex.Message, StatusCodes.Status500InternalServerError);
            }
        }

        public async Task<Result<ProductDto>> Create(CreateProductRequest request)
        {
           
            try
            {
                var validator = new BaseProductValidator(_context);
                var validationResult = await validator.ValidateAsync(request);

                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                    return Result<ProductDto>.Failure(errors, StatusCodes.Status400BadRequest);
                }

                var uploadedImages = new List<string>();
                //foreach (var image in request.Images ?? new List<string>())
                //{
                //    if (!BeValidImage(image))
                //    {
                //        return Result<ProductDto>.Failure($"Invalid image: {image}", StatusCodes.Status400BadRequest);
                //    }

                //    var uploadResult = await CloudinaryExtension.UploadImageToCloudinary(image,_cloudinary);
                //    if (uploadResult != null)
                //    {
                //        uploadedImages.Add(uploadResult.SecureUrl.AbsoluteUri);
                //    }
                //}
                var product = _mapper.Map<Product>(request);
                product.Type = ProductType.Option;
                product.Status = ProductStatus.Draft;
                product.Quantity = 0;
                product.Images = _mapper.Map<String>(request.Images);// string.Join(",", request.Images);
                product.Id = 0;




                //product.InternalCode = request.InternalCode;
                //product.Name = request.Name;
                //product.Price = request.Price;
                //product.Describes = request.Describes;
                //product.Feature = request.Feature;
                //product.Specifications = request.Specifications;
                //product.CategoryId = request.CategoryId;

                var newProduct = await _context.Products.AddAsync(product);
                await _context.SaveChangesAsync();

                var productDto = _mapper.Map<ProductDto>(newProduct.Entity);

                return Result<ProductDto>.Success(productDto, StatusCodes.Status201Created);
            }
            catch (Exception ex)
            {
                return Result<ProductDto>.Failure($"An error occurred: {ex.Message}", StatusCodes.Status500InternalServerError);
            }
        }


        public async Task<Result<ProductDto>> Update(UpdateProductRequest request)
        {
            try
            {
                var validator = new UpdateBaseValidator<UpdateProductRequest>(_context);
                var validationResult = await validator.ValidateAsync(request);

                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                    return Result<ProductDto>.Failure(errors, StatusCodes.Status400BadRequest);
                }

                var findEntityProduct = await _context.Set<Product>().FindAsync(request.Id);

                if (findEntityProduct is null)
                {
                    throw new BadRequestException(ValidatorTransform.NotExistsValue(Modules.Product.Module,
                                    request.Id.ToString()));
                }
                findEntityProduct.CopyPropertiesFrom(request);

                if(request.Images!=null)
                findEntityProduct.Images = _mapper.Map<string>(request.Images);

            

                _context.Products.Update(findEntityProduct);
                await _context.SaveChangesAsync();

                var productDto = _mapper.Map<ProductDto>(findEntityProduct);

                return Result<ProductDto>.Success(productDto, StatusCodes.Status200OK);
            }
            catch (Exception ex)
            {
                return Result<ProductDto>.Failure($"An error occurred: {ex.Message}", StatusCodes.Status500InternalServerError);
            }
        }
        public async Task<Result<bool>> Delete(int id)
        {
            try
            {
                var entity = await _context.Set<Product>().FirstOrDefaultAsync(x => x.Id ==id && x.IsDeleted == false);

                if (entity == null)
                    throw new NotFoundException(Modules.Product.Module, id.ToString());

                _context.Set<Product>().Remove(entity);

                await _context.SaveChangesAsync();

         
                return Result<bool>.Success(true, StatusCodes.Status200OK);
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure($"An error occurred: {ex.Message}", StatusCodes.Status500InternalServerError);
            }
        }

        public async Task<Result<ProductDto>> Detail(DetailBaseCommand request)
        {
            string cacheKey = $"product:detail:{request.Id}"; // Key cho Redis

            // Thử lấy từ cache
            var cachedProductDto = await _redisCacheService.GetAsync<ProductDto>(cacheKey);

            if (cachedProductDto != null)
            {
                _logger.LogInformation("Returning product detail from cache for ID {ProductId}", request.Id);
                return Result<ProductDto>.Success(cachedProductDto, StatusCodes.Status200OK);
            }
            try
            {
                var validator = new DetailBaseValidator(_context);
                var validationResult = await validator.ValidateAsync(request);

                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                    return Result<ProductDto>.Failure(errors, StatusCodes.Status400BadRequest);
                }
                var product = _context.Set<Product>().FilterDeleted().Where(x => x.Id == request.Id);


                if (request.IsAllDetail)
                {
                    product = product.Include(x => x.Category);
                    product = product.Include(x => x.Parent);
                }

                if (_currentUserService.Type != CLAIMS_VALUES.TYPE_ADMIN &&
                    _currentUserService.Type != CLAIMS_VALUES.TYPE_SUPER_ADMIN)
                {
                    product = product.Where(x => x.Status == Product.ProductStatus.Active);
                }

                product = product.Where(x => x.Type == Product.ProductType.Option);

                var findEntityProduct = await product.SingleOrDefaultAsync();




                if (findEntityProduct is null)
                {
                    return Result<ProductDto>.Failure(
                        ValidatorTransform.NotExistsValue(Modules.Id, request.Id.ToString()),
                        StatusCodes.Status404NotFound);
                }


                var productDto = _mapper.Map<ProductDto>(findEntityProduct);


                (Promotion promo, decimal? priceDiscoutMax, int? group) =
                        await BaseOrderApplyPromotion.
                            ApplyPromotionForSingleProduct(_context, findEntityProduct);

                productDto.NewPrice = findEntityProduct.Price - priceDiscoutMax;
                productDto.PromotionDto = _mapper.Map<PromotionDto>(promo);

                // Lưu vào cache
                await _redisCacheService.SetAsync(cacheKey, productDto, _cacheExpiry);
                _logger.LogInformation("Caching product detail for ID {ProductId}", request.Id);


                return Result<ProductDto>.Success(productDto, StatusCodes.Status200OK);
            }
            catch (Exception ex)
            {
                return Result<ProductDto>.Failure($"An error occurred: {ex.Message}", StatusCodes.Status500InternalServerError);
            }
        }

        public async Task<PaginatedResult<List<ProductDto>>> GetList(ListBaseCommand request)
        {
            //string cacheKey = $"product:list:{JsonConvert.SerializeObject(request)}"; // Key cho Redis, include request params
            // Tạo cache key
           await SyncInventoryToRedisAsync();
            string cacheKey = $"product:list:{request.Page}:{request.PageSize}:{request.Filters}:{request.Sorts}";

            // Thử lấy dữ liệu từ cache
            var cachedResult = await _redisCacheService.GetAsync<PaginatedResult<List<ProductDto>>>(cacheKey);
            if (cachedResult != null)
            {
                _logger.LogInformation("Returning product list from cache");
                return cachedResult;
            }

            try
            {

                var validator = new ListBaseCommandValidator(_context);
                var validationResult = await validator.ValidateAsync(request);

                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                    return PaginatedResult<List<ProductDto>>.Failure(StatusCodes.Status400BadRequest, errors);
                }



                var query = _context.Set<Product>().FilterDeleted();

                var sieveModel = _mapper.Map<SieveModel>(request);
                //var sieveModel = new SieveModel
                //{
                //    Page = request.Page,
                //    PageSize = request.PageSize,
                //    Filters = request.Filters,
                //    Sorts = request.Sorts
                //};
                if (_currentUserService.Type != CLAIMS_VALUES.TYPE_ADMIN &&
           _currentUserService.Type != CLAIMS_VALUES.TYPE_SUPER_ADMIN)
                {
                    query = query.Where(x => x.Status == Product.ProductStatus.Active);
                }

                if (request.IsAllDetail)
                {
                    query = query.Include(x => x.Category);
                    query = query.Include(x => x.Parent);
                }

                query = query.Where(x => x.Type == Product.ProductType.Option);

                int totalCount = await PaginatedResultBase.CountApplySieveAsync(_sieveProcessor, sieveModel, query);


                var filteredQuery = _sieveProcessor.Apply(sieveModel, query);

                //var totalCount = await filteredQuery.CountAsync();



                var products = await filteredQuery
                    .Skip((request.Page.Value - 1) * request.PageSize.Value)
                    .Take(request.PageSize.Value)
                    .ToListAsync();

                var productDtos = _mapper.Map<List<ProductDto>>(products);


                if (request.IsAllDetail)
                {
                    for (int i = 0; i < productDtos.Count; i++)
                    {
                        var product = _mapper.Map<Product>(productDtos[i]);

                        (Promotion promo, decimal? priceDiscoutMax, int? group) =
                                await BaseOrderApplyPromotion.
                                    ApplyPromotionForSingleProduct(_context, product);

                        productDtos[i].NewPrice = product.Price - priceDiscoutMax;
                        productDtos[i].PromotionDto = _mapper.Map<PromotionDto>(promo);
                    }
                }

                var paginatedResult = PaginatedResult<List<ProductDto>>.Create(productDtos, totalCount, request.Page.Value, request.PageSize.Value, StatusCodes.Status200OK);
                await _redisCacheService.SetAsync(cacheKey, paginatedResult, TimeSpan.FromMinutes(30));
                _logger.LogInformation($"Storing product list in cache. Key: {cacheKey}");

                return paginatedResult;
            }
            catch (Exception ex)
            {
                return PaginatedResult<List<ProductDto>>.Failure(StatusCodes.Status500InternalServerError, new List<string> { ex.Message });
            }
        }


        public async Task<PaginatedResult<List<PromotionComboProductDto>>> GetListPromotionComBo(ListBaseCommand request)
        {

            try
            {
                var validator = new ListBaseCommandValidator(_context);
                var validationResult = await validator.ValidateAsync(request);

                if (!validationResult.IsValid)
                {
                    var errorMessages = validationResult.Errors.Select(x => x.ErrorMessage).ToList();
                    return PaginatedResult<List<PromotionComboProductDto>>.Failure(StatusCodes.Status400BadRequest, errorMessages);
                }
                var query = _context.PromotionProductRequirements
                    .Include(x => x.Promotion)
                    .Include(x => x.Product)
                    .Where(x => x.Promotion.Start <= DateTime.Now &&
                                    DateTime.Now <= x.Promotion.End &&
                                    x.Promotion.Limit >= 1 &&
                                    x.Promotion.Status == PromotionStatus.Approve)
                    .GroupBy(x => x.Group)
                    .AsQueryable();

                request.PageSize = 1000;
                var sieve = _mapper.Map<SieveModel>(request);

                int totalCount = await PaginatedResultBase.CountApplySieveAsync(_sieveProcessor, sieve, query);

                var temp = await query.ToListAsync();

                var results = temp
                    .Select(g => new PromotionComboProductDto
                    {
                        Id = g.Key,
                        Products = g.Select(p => _mapper.Map<ProductDto>(p.Product)).ToList(),
                        Promotion = _mapper.Map<PromotionDto>(g.Select(p => p.Promotion).FirstOrDefault()),
                    })
                    .ToList();
                List<PromotionComboProductDto> results2 = new List<PromotionComboProductDto>();
                for (int i = 0; i < results.Count; i++)
                {
                    int number = results[i].Products.Count();
                    if (number == 1)
                    {
                        continue;
                    }
                    decimal? price = 0;
                    decimal? priceDiscout = 0;
                    for (int j = 0; j < results[i].Products.Count(); j++)
                    {
                        price += results[i].Products[j].Price;
                        if (results[i].Promotion.Type == PromotionType.Percent)
                        {
                            priceDiscout = results[i].Products[j].Price * (results[i].Promotion.Percent * 0.01m) > results[i].Promotion.DiscountMax ?
                                            results[i].Promotion.DiscountMax : results[i].Products[j].Price * (results[i].Promotion.Percent * 0.01m);
                            results[i].Products[j].NewPrice = results[i].Products[j].Price - priceDiscout / number;
                        }
                        else if (results[i].Promotion.Type == PromotionType.Discount)
                        {
                            priceDiscout = (results[i].Promotion.Discount / number) > results[i].Products[j].Price * (results[i].Promotion.PercentMax * 0.01m) ?
                                            results[i].Products[j].Price * (results[i].Promotion.PercentMax * 0.01m) : (results[i].Promotion.Discount / number);
                            results[i].Products[j].NewPrice = results[i].Products[j].Price - priceDiscout / number;
                        }
                    }
                    results[i].Price = price;
                    results[i].ReducedPrice = priceDiscout;
                    results[i].NewPrice = price - priceDiscout;
                    results2.Add(results[i]);
                }

                return PaginatedResult<List<PromotionComboProductDto>>.Success(results2, totalCount, request.Page, request.PageSize);
            }
            catch (Exception ex)
            {
                return PaginatedResult<List<PromotionComboProductDto>>.Failure(StatusCodes.Status500InternalServerError,
                    new List<string> { ex.Message });
            }
        }


    }
}
