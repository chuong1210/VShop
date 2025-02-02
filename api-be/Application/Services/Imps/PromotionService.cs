using api_be.Domain.Constants;
using api_be.Infrastructure.Services;
using api_be.Core.Domain.Interfaces;
using api_be.Core.Entities;
using api_be.Domain.Exceptions;
using api_be.Domain.Extensions;
using api_be.Domain.Models.Request.PromotionRequest;
using api_be.Domain.Models.Responses;
using api_be.Domain.DefaultValidatorBase;
using api_be.Application.ValidatorRequest.OrderValidator.BaseOrders;
using api_be.Application.ValidatorRequest.PromotionValidator;
using api_be.Application.ValidatorRequest.PromotionValidator.BasePromotion;
using api_be.Application.ValidatorRequest.RoleValidator;
using api_be.Domain.Transforms;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Sieve.Models;
using Sieve.Services;
using System.Threading;
using Twilio.TwiML.Voice;
using static api_be.Core.Entities.Promotion;
using api_be.Infrastructure.DB;
using Microsoft.Extensions.Configuration;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using api_be.Middleware;
using Microsoft.Extensions.DependencyInjection;

namespace api_be.Application.Services.Imps
{
    [RegisterService(ServiceLifetime.Scoped)]
    public class PromotionService : IPromotionService
    {
        private readonly ISupermarketDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IMapper _mapper;
        private readonly ISieveProcessor _sieveProcessor;
        private readonly ICurrentUserService _currentuserService;
        public PromotionService(ISupermarketDbContext pContext, IConfiguration pConfiguration, IMapper pMapper, ISieveProcessor pSieveProcessor, ICurrentUserService currentuserService)
        {
            _context = pContext;
            _configuration = pConfiguration;
            _mapper = pMapper;
            _sieveProcessor = pSieveProcessor;
            _currentuserService = currentuserService;
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

        public async Task<Result<PromotionDto>> ChangeStatus(ChangeStatusPromotionRequest request)
        {
            var validator = new ChangeStatusPromotionValidator(_context);
            var validationResult = await validator.ValidateAsync(request);

            if (validationResult.IsValid == false)
            {
                var errorMessages = validationResult.Errors.Select(x => x.ErrorMessage).ToList();
                return Result<PromotionDto>.Failure(errorMessages, StatusCodes.Status400BadRequest);
            }

            var findEntity = await _context.Promotions.FindAsync(request.PromotionId);

            bool flag1 = findEntity.Status == PromotionStatus.Draft &&
                (request.Status == PromotionStatus.Approve || request.Status == PromotionStatus.Cancel);
            bool flag2 = findEntity.Status == PromotionStatus.Approve &&
                (request.Status == PromotionStatus.Draft || request.Status == PromotionStatus.Cancel);

            if (!flag1 && !flag2)
            {
                return Result<PromotionDto>.Failure("Trạng thái không hợp lệ!", StatusCodes.Status400BadRequest);
            }

            findEntity.Status = request.Status;

            var newEntity = _context.Promotions.Update(findEntity);
            await _context.SaveChangesAsync();

            var dto = _mapper.Map<PromotionDto>(newEntity.Entity);

            return Result<PromotionDto>.Success(dto, StatusCodes.Status200OK);
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
            try
            {
                var promotion = await _context.Promotions
                                    .FirstOrDefaultAsync(pr => pr.Id == id);

                if (promotion == null)
                {
                    return Result<bool>.Failure(ValidatorTransform.NotExists(Modules.Promotion.Module), StatusCodes.Status404NotFound);
                }

    

                // Xóa Role
                _context.Set<Promotion>().Remove(promotion);

                await _context.SaveChangesAsync();

                return Result<bool>.Success(true, StatusCodes.Status200OK);
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure($"An error occurred: {ex.Message}", StatusCodes.Status500InternalServerError);
            }
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
            try
            {
                var validator = new ListBaseCommandValidator(_context);
                var validationResult = await validator.ValidateAsync(request);

                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                    return PaginatedResult<List<PromotionDto>>.Failure(StatusCodes.Status400BadRequest, errors);
                }



                var query = _context.Set<Promotion>().FilterDeleted();

                var sieveModel = _mapper.Map<SieveModel>(request);
                //var sieveModel = new SieveModel
                //{
                //    Page = request.Page,
                //    PageSize = request.PageSize,
                //    Filters = request.Filters,
                //    Sorts = request.Sorts
                //};
       


                int totalCount = await PaginatedResultBase.CountApplySieveAsync(_sieveProcessor, sieveModel, query);


                var filteredQuery = _sieveProcessor.Apply(sieveModel, query);

                //var totalCount = await filteredQuery.CountAsync();



                var promotions = await filteredQuery
                    .Skip((request.Page.Value - 1) * request.PageSize.Value)
                    .Take(request.PageSize.Value)
                    .ToListAsync();

                var promotionDtos = _mapper.Map<List<PromotionDto>>(promotions);


        


                return PaginatedResult<List<PromotionDto>>.Create(promotionDtos, totalCount, request.Page.Value, request.PageSize.Value, StatusCodes.Status200OK);
            }
            catch (Exception ex)
            {
                return PaginatedResult<List<PromotionDto>>.Failure(StatusCodes.Status500InternalServerError, new List<string> { ex.Message });
            }
        }

 
        public async Task<Result<PromotionDto>> Update(CreateOrUpdatePromotionRequest request)
        {
            var validator = new BasePromotionValidator(_context);
            var validationResult = await validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return Result<PromotionDto>.Failure(errors, StatusCodes.Status400BadRequest);
            }

            var findEntity = await _context.Set<Promotion>().FindAsync(request.Id);

            if (findEntity == null)
            {
                throw new BadRequestException(ValidatorTransform.NotExistsValue(Modules.Promotion.Module,
                                request.Id.ToString()));
            }
            findEntity.CopyPropertiesFrom(request);


            var newEntity = _context.Set<Promotion>().Update(findEntity);
            await _context.SaveChangesAsync();

            var promotionDto = _mapper.Map<PromotionDto>(newEntity.Entity);

            return (Result<PromotionDto>.Success(promotionDto, StatusCodes.Status200OK));



        }
    }
}
