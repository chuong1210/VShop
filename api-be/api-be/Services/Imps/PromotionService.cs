using api_be.Domain.Interfaces;
using api_be.Entities;
using api_be.Extensions;
using api_be.Models.Request.PromotionRequest;
using api_be.Models.Responses;
using api_be.Models.ValidatorRequest.DefaultBase;
using api_be.Models.ValidatorRequest.PromotionValidator;
using api_be.Models.ValidatorRequest.PromotionValidator.BasePromotion;
using api_be.Models.ValidatorRequest.RoleValidator;
using api_be.Transforms;
using api_be.ValidatorRequest.DefaultBase;
using Microsoft.EntityFrameworkCore;
using Sieve.Services;
using System.Threading;
using static api_be.Entities.Promotion;

namespace api_be.Services.Imps
{
    public class PromotionService : IPromotionService
    {
        private readonly ISupermarketDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IMapper _mapper;
        private readonly ISieveProcessor _sieveProcessor;
        public PromotionService(ISupermarketDbContext pContext, IConfiguration pConfiguration, IMapper pMapper, ISieveProcessor pSieveProcessor)
        {
            _context = pContext;
            _configuration = pConfiguration;
            _mapper = pMapper;
            _sieveProcessor = pSieveProcessor;
        }
        public async Task<Result<bool>> ApplyPromotionForProduct(ApplyPromotionForProductRequest request)
        {

            var validator = new ApplyPromotionForProductValidator(_context);
            var validationResult = await validator.ValidateAsync(request);

            if (validationResult.IsValid == false)
            {
                var errorMessages = validationResult.Errors.Select(x => x.ErrorMessage).ToList();
                return Result<bool>.Failure(errorMessages, StatusCodes.Status400BadRequest);
            }

            if (request.Group != -1)
            {
                // Cập nhật lại danh sách khuyến mãi cũ                                                                                                                  
                var productsId = await _context.PromotionProductRequirements
                                    .Where(x => x.Group == request.Group)
                                    .Select(x => x.ProductId)
                                    .ToListAsync();

                var productIdDelete = productsId.Cast<int>().Except(request.ProductsId).ToList();
                var productIdCreate = request.ProductsId.Except(productsId.Cast<int>()).ToList();

                foreach (var productId in productIdDelete)
                {
                    var detail = await _context.PromotionProductRequirements
                        .Where(x => x.PromotionId == request.PromotionId && x.ProductId == productId &&
                        x.Group == request.Group)
                        .FirstOrDefaultAsync();
                    _context.PromotionProductRequirements.Remove(detail);
                }
                await _context.SaveChangesAsync();

                foreach (var productId in productIdCreate)
                {
                    var detail = new PromotionProductRequirement
                    {
                        PromotionId = request.PromotionId,
                        ProductId = productId,
                        Group = request.Group,
                    };
                    await _context.PromotionProductRequirements.AddAsync(detail);
                }
                await _context.SaveChangesAsync();
            }
            else
            {
                // Tạo mới 1 group sản phẩm để áp dụng khuyến mãi
                var maxGroup = await _context.PromotionProductRequirements.MaxAsync(x => x.Group);
                int? newGroup = maxGroup == null ? 0 : maxGroup + 1;
                foreach (var productId in request.ProductsId)
                {
                    var detail = new PromotionProductRequirement
                    {
                        PromotionId = request.PromotionId,
                        ProductId = productId,
                        Group = newGroup
                    };
                    await _context.PromotionProductRequirements.AddAsync(detail);
                }
                await _context.SaveChangesAsync();
            }

            return Result<bool>.Success(true, StatusCodes.Status200OK);
        }

        public async Task<Result<bool>> ChangeStatus(ChangeStatusPromotionRequest request)
        {
            throw new NotImplementedException();
        }

        public async Task<Result<PromotionDto>> Create(CreateOrUpdatePromotionRequest request)
        {
            var promotion = _mapper.Map<Promotion>(request);
            promotion.Status = PromotionStatus.Draft;

            if (promotion.Type == PromotionType.Discount)
            {
                promotion.Percent = 0;
                promotion.DiscountMax = 0;
            }
            if (promotion.Type == PromotionType.Percent)
            {
                promotion.Discount = 0;
                promotion.PercentMax = 0;
            }

            try
            {
                var validator = new BasePromotionValidator(_context, null);
                var validationResult = await validator.ValidateAsync(request);

                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                    return Result<PromotionDto>.Failure(errors, StatusCodes.Status400BadRequest);
                }

                var promotionEntitiy = _mapper.Map<Promotion>(request);
                var newPromotion = await _context.Set<Promotion>().AddAsync(promotionEntitiy);
                await _context.SaveChangesAsync();

                var roleDto = _mapper.Map<PromotionDto>(newPromotion.Entity);
                return Result<PromotionDto>.Success(roleDto, StatusCodes.Status201Created);
            }
            catch (Exception ex)
            {
                return Result<PromotionDto>.Failure($"An error occurred: {ex.Message}", StatusCodes.Status500InternalServerError);
            }

        }

        public async Task<Result<bool>> Delete(int id)
        {
            throw new NotImplementedException();
        }

        public async Task<Result<PromotionDto>> Detail(DetailBaseCommand request)
        {
            try
            {
                // Validate ID
                var validator = new DetailBaseValidator(_context);
                var validationResult = await validator.ValidateAsync(request);

                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                    return Result<PromotionDto>.Failure(errors, StatusCodes.Status400BadRequest);
                }


                var promotion = _context.Set<Promotion>().FilterDeleted().Where(x => x.Id == request.Id);

                var findEntity = await promotion.SingleOrDefaultAsync();

                if (findEntity is null)
                {
                    return Result<PromotionDto>.Failure(ValidatorTransform.NotExistsValue(Modules.Id, request.Id.ToString()),
                     StatusCodes.Status404NotFound);
                }



                // Map to DTO

                var PromotionDto = _mapper.Map<PromotionDto>(findEntity);
                PromotionDto.PromotionForProduct = await _context.PromotionProductRequirements
                   .Where(x => x.PromotionId == PromotionDto.Id)
                   .Include(x => x.Product)
                   .GroupBy(x => x.Group)
                   .Select(x => new PromotionForProductDto
                   {
                       Group = x.Key,
                       GroupProducts = x.Select(y => y.ProductId).ToList(),
                   })
                   .ToListAsync();

                return Result<PromotionDto>.Success(PromotionDto, StatusCodes.Status200OK);
            }
            catch (Exception ex)
            {
                return Result<PromotionDto>.Failure($"An error occurred: {ex.Message}", StatusCodes.Status500InternalServerError);
            }

     
        }

        public async Task<PaginatedResult<List<PromotionDto>>> GetList(ListBaseCommand request)
        {
            throw new NotImplementedException();
        }

        public async Task<PaginatedResult<List<PromotionDto>>> GetListPromotionComBo(ListBaseCommand request)
        {
            throw new NotImplementedException();
        }

        public async Task<Result<PromotionDto>> Update(CreateOrUpdatePromotionRequest request)
        {
            throw new NotImplementedException();
        }
    }
}
