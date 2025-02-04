using api_be.Domain.Models.Request.CouponRequest;
using api_be.Domain.Models.Responses;
using api_be.Application.ValidatorRequest.CouponValidator;
using api_be.Domain.DefaultValidatorBase;
using Microsoft.EntityFrameworkCore;
using static api_be.Core.Entities.Coupon;
using System.Threading;
using api_be.Core.Domain.Interfaces;
using CloudinaryDotNet;
using Sieve.Services;
using api_be.Domain.Extensions;
using api_be.Core.Entities;
using  api_be.Application.ValidatorRequest.BaseProduct;
using Twilio.TwiML.Voice;
using api_be.Domain.Transforms;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using Sieve.Models;
using api_be.Domain.Constants;
using api_be.Domain.Exceptions;
using api_be.Application.ValidatorRequest.RoleValidator;
using api_be.Application.ValidatorRequest.CouponValidator.BaseCoupon;
using api_be.Infrastructure.DB;
using Microsoft.AspNetCore.Http;
using AutoMapper;

namespace api_be.Application.Services.Imps
{
    public class CouponService : ICouponService
    {
        private readonly ISupermarketDbContext _context;
        private readonly IMapper _mapper;
        private readonly ISieveProcessor _sieveProcessor;
        private readonly ICurrentUserService _currentUserService;


        public CouponService(ISupermarketDbContext context, IMapper mapper, ISieveProcessor sieveProcessor, ICurrentUserService currentUserService)
        {
            _context = context;
            _mapper = mapper;
            _sieveProcessor = sieveProcessor;
       
            _currentUserService = currentUserService;
        }
        public async Task<Result<CouponDto>> ChangeStatus(ChangeStatusCouponRequest request)
        {
            var validator = new ChangeStatusCouponValidator(_context);
            var validationResult = await validator.ValidateAsync(request);

            if (validationResult.IsValid == false)
            {
                var errorMessages = validationResult.Errors.Select(x => x.ErrorMessage).ToList();
                return Result<CouponDto>.Failure(errorMessages, StatusCodes.Status400BadRequest);
            }

            var findEntity = await _context.Coupons.FindAsync(request.CouponId);

            bool flag1 = findEntity.Status == CouponStatus.Draft &&
                (request.Status == CouponStatus.Approve || request.Status == CouponStatus.Cancel);
            bool flag2 = findEntity.Status == CouponStatus.Approve &&
                (request.Status == CouponStatus.Draft || request.Status == CouponStatus.Cancel);

            if (!flag1 && !flag2)
            {
                return Result<CouponDto>.Failure("Trạng thái không hợp lệ!", StatusCodes.Status400BadRequest);
            }
            findEntity.Status = request.Status;

            var newEntity = _context.Coupons.Update(findEntity);
            await _context.SaveChangesAsync();

            var dto = _mapper.Map<CouponDto>(newEntity.Entity);

            return Result<CouponDto>.Success(dto, StatusCodes.Status200OK);
        }

        public async Task<Result<CouponDto>> Create(CreateOrUpdateCopuponRequest request)
        {
            var validator = new BaseCouponValidator(_context);
            var validationResult = await validator.ValidateAsync(request);

            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return Result<CouponDto>.Failure(errors, StatusCodes.Status400BadRequest);
            }
            var coupon = _mapper.Map<Coupon>(request);
            coupon.Status = CouponStatus.Draft;

            if (coupon.Type == CouponType.Discount)
            {
                coupon.Percent = 0;
                coupon.DiscountMax = 0;
            }
            if (coupon.Type == CouponType.Percent)
            {
                coupon.Discount = 0;
                coupon.PercentMax = 0;
            }
            if (coupon.TypeC == CType.SC)
            {
                coupon.CustomerId = null;
            }

            coupon.Id = 0;
            var newCoupon = await _context.Set<Coupon>().AddAsync(coupon);
            await _context.SaveChangesAsync();

            var couponDto = _mapper.Map<CouponDto>(newCoupon.Entity);

            return Result<CouponDto>.Success(couponDto, StatusCodes.Status201Created);
        }

        public async Task<Result<bool>> Delete(int id)
        {
            try
            {
                var coupon = await _context.Set<Coupon>()
                                    .FirstOrDefaultAsync(pr => pr.Id == id);

                if (coupon == null)
                {
                    return Result<bool>.Failure(ValidatorTransform.NotExists(Modules.Coupon.Module), StatusCodes.Status404NotFound);
                }



                // Xóa Role
                _context.Set<Coupon>().Remove(coupon);

                await _context.SaveChangesAsync();

                return Result<bool>.Success(true, StatusCodes.Status200OK);
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure($"An error occurred: {ex.Message}", StatusCodes.Status500InternalServerError);
            }
        }
        public async Task<Result<CouponDto>> Detail(DetailBaseCommand request)
        {
            try
            {
                // Validate ID
                var validator = new DetailBaseValidator(_context);
                var validationResult = await validator.ValidateAsync(request);

                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                    return Result<CouponDto>.Failure(errors, StatusCodes.Status400BadRequest);
                }


                var coupon = _context.Set<Coupon>().FilterDeleted().Where(x => x.Id == request.Id);
                if (request.IsAllDetail)
                {
                    coupon = coupon.Include(x => x.Customer);
                }
                var findEntity = await coupon.SingleOrDefaultAsync();

                if (findEntity is null)
                {
                    return Result<CouponDto>.Failure(ValidatorTransform.NotExistsValue(Modules.Id, request.Id.ToString()),
                     StatusCodes.Status404NotFound);
                }


               

                // Map to DTO

                var couponDto = _mapper.Map<CouponDto>(findEntity);
        

                return Result<CouponDto>.Success(couponDto, StatusCodes.Status200OK);
            }
            catch (Exception ex)
            {
                return Result<CouponDto>.Failure($"An error occurred: {ex.Message}", StatusCodes.Status500InternalServerError);
            }
        }

        public async Task<PaginatedResult<List<CouponDto>>> GetList(ListBaseCommand request)
        {
            try
            {
                var validator = new ListBaseCommandValidator(_context);
                var validationResult = await validator.ValidateAsync(request);

                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                    return PaginatedResult<List<CouponDto>>.Failure(StatusCodes.Status400BadRequest, errors);

                }
                var query = _context.Set<Coupon>().FilterDeleted();

                if (request.IsAllDetail)
                {
                    query = query.Include(x => x.Customer);
                }

                if (_currentUserService.Type == CLAIMS_VALUES.TYPE_USER)
                {
                    query = query
                        .Where(x => x.CustomerId == _currentUserService.CustomerId ||
                                    x.CustomerId == null);
                }




                // Apply Sieve
                var sieveModel = new SieveModel
                {
                    Page = request.Page,
                    PageSize = request.PageSize,
                    Filters = request.Filters
                };

                sieveModel = _mapper.Map<SieveModel>(request);




                var totalCount = await PaginatedResultBase.CountApplySieveAsync(_sieveProcessor, sieveModel, query);


                var paginatedQuery = _sieveProcessor.Apply(sieveModel, query);

                var coupons = await paginatedQuery.Skip((request.Page.Value - 1) * request.PageSize.Value)
                                                .Take(request.PageSize.Value)
                                                .ToListAsync();



                var couponDtos = _mapper.Map<List<CouponDto>>(coupons);
                var paginatedResult = PaginatedResult<List<CouponDto>>.Create(couponDtos, totalCount, request.Page.Value, request.PageSize.Value, StatusCodes.Status200OK);

                return paginatedResult;
            }
            catch (Exception ex)
            {
                return PaginatedResult<List<CouponDto>>.Failure(StatusCodes.Status500InternalServerError, new List<string> { ex.Message });
            }
        }

        public async Task<Result<CouponDto>> Update(CreateOrUpdateCopuponRequest request)
        {
            try
            {
                var validator = new BaseCouponValidator(_context, null);
                var validationResult = await validator.ValidateAsync(request);
                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                    return Result<CouponDto>.Failure(errors, StatusCodes.Status400BadRequest);
                }



                var coupon = await _context.Set<Coupon>().FindAsync(request.Id);

                if (coupon == null)
                {
                    throw new BadRequestException(ValidatorTransform.NotExistsValue(Modules.Coupon.Module,
                                    request.Id.ToString()));
                }

                coupon.CopyPropertiesFrom(request);




                var newEntity = _context.Set<Coupon>().Update(coupon);
                await _context.SaveChangesAsync();

                var couponDto = _mapper.Map<CouponDto>(newEntity.Entity);

                return (Result<CouponDto>.Success(couponDto, StatusCodes.Status200OK));

            }
            catch (Exception ex)
            {
                // Trả về lỗi nếu có exception
                return Result<CouponDto>.Failure($"Đã có lỗi xảy ra: {ex.Message}", StatusCodes.Status500InternalServerError);
            }
        }
    }
}
