using api_be.Core.Domain.Interfaces;
using api_be.Core.Entities;
using api_be.Domain.Exceptions;
using api_be.Domain.Extensions;
using api_be.Application.Models.Request.PaymentRequest;
using api_be.Application.Models.ValidatorRequest.CouponValidator.BaseCoupon;
using api_be.Application.Models.ValidatorRequest.PaymentValidator.BasePayment;
using api_be.Domain.Transforms;
using api_be.Application.Models.ValidatorRequest.BaseCategory;
using Microsoft.EntityFrameworkCore;
using Sieve.Models;
using Sieve.Services;
using api_be.Infrastructure.DB;
using Microsoft.Extensions.Configuration;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using api_be.Middleware;
using Microsoft.Extensions.DependencyInjection;
using api_be.Domain.ResultResponses;
using api_be.Application.Models.ValidatorRequest.DefaultValidatorBase;
using api_be.Application.Responses.PaymentResponse;

namespace api_be.Application.Services.PaymentService
{
    [RegisterService(ServiceLifetime.Scoped)]

    public class PaymentService : IPaymentService
    {
        private readonly ISupermarketDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IMapper _mapper;
        private readonly ISieveProcessor _sieveProcessor;


        public PaymentService(ISupermarketDbContext pContext, IConfiguration pConfiguration, IMapper pMapper, ISieveProcessor pSieveProcessor)
        {
            _context = pContext;
            _configuration = pConfiguration;
            _mapper = pMapper;
            _sieveProcessor = pSieveProcessor;

        }
        public async Task<Result<PaymentDto>> Create(CreateOrUpdatePaymentRequest request)
        {
            try
            {
                var validator = new BasePaymentValidator(_context);
                var validationResult = await validator.ValidateAsync(request);

                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                    return Result<PaymentDto>.Failure(errors, StatusCodes.Status400BadRequest);
                }

                var payment = _mapper.Map<Payment>(request);
                var newPayment = await _context.Set<Payment>().AddAsync(payment);
                await _context.SaveChangesAsync();

                var paymentDto = _mapper.Map<PaymentDto>(newPayment.Entity);

                return Result<PaymentDto>.Success(paymentDto, StatusCodes.Status201Created);
            }
            catch (Exception ex)
            {
                return Result<PaymentDto>.Failure($"An error occurred: {ex.Message}", StatusCodes.Status500InternalServerError);
            }
        }

        public async Task<Result<bool>> Delete(int id)
        {
            try
            {

                if (id == null || id <= 0)
                {
                    return Result<bool>.Failure(ValidatorTransform.Required(Modules.Payment.Module), StatusCodes.Status400BadRequest);
                    //throw new BadRequestException(string.Join(",", ex.Message));

                }

                var Payment = await _context.Set<Payment>().FindAsync(id);
                if (Payment == null)
                {
                    throw new NotFoundException(Modules.Payment.Id, id.ToString());
                }

                _context.Set<Payment>().Remove(Payment);
                await _context.SaveChangesAsync();
                return Result<bool>.Success(true, StatusCodes.Status200OK);
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure($"An error occurred: {ex.Message}", StatusCodes.Status500InternalServerError);
            }
        }

        public async Task<Result<PaymentDto>> Detail(DetailBaseCommand request)
        {
            try
            {
                // Validate ID
                var validator = new DetailBaseValidator(_context);
                var validationResult = await validator.ValidateAsync(request);

                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                    return Result<PaymentDto>.Failure(errors, StatusCodes.Status400BadRequest);
                }


                var Payment = _context.Set<Payment>().FilterDeleted().Where(x => x.Id == request.Id);
                var findEntity = await Payment.SingleOrDefaultAsync();

                if (findEntity is null)
                {
                    return Result<PaymentDto>.Failure(ValidatorTransform.NotExistsValue(Modules.Id, request.Id.ToString()),
                     StatusCodes.Status404NotFound);
                }




                // Map to DTO

                var PaymentDto = _mapper.Map<PaymentDto>(findEntity);


                return Result<PaymentDto>.Success(PaymentDto, StatusCodes.Status200OK);
            }
            catch (Exception ex)
            {
                return Result<PaymentDto>.Failure($"An error occurred: {ex.Message}", StatusCodes.Status500InternalServerError);
            }
        }

        public async Task<PaginatedResult<List<PaymentDto>>> GetList(ListBaseCommand request)
        {
            try
            {
                var validator = new ListBaseCommandValidator(_context);
                var validationResult = await validator.ValidateAsync(request);

                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                    return PaginatedResult<List<PaymentDto>>.Failure(StatusCodes.Status400BadRequest, errors);

                }
                var query = _context.Set<Payment>().FilterDeleted();







                var sieveModel = _mapper.Map<SieveModel>(request);




                var totalCount = await PaginatedResultBase.CountApplySieveAsync(_sieveProcessor, sieveModel, query);


                var paginatedQuery = _sieveProcessor.Apply(sieveModel, query);

                var Payments = await paginatedQuery.ToListAsync();



                var PaymentDtos = _mapper.Map<List<PaymentDto>>(Payments);
                var paginatedResult = PaginatedResult<List<PaymentDto>>.Create(PaymentDtos, totalCount, request.Page.Value, request.PageSize.Value, StatusCodes.Status200OK);

                return paginatedResult;
            }
            catch (Exception ex)
            {
                return PaginatedResult<List<PaymentDto>>.Failure(StatusCodes.Status500InternalServerError, new List<string> { ex.Message });
            }
        }

        public async Task<Result<PaymentDto>> Update(CreateOrUpdatePaymentRequest request)
        {
            try
            {
                var validator = new BasePaymentValidator(_context, null);
                var validationResult = await validator.ValidateAsync(request);
                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                    return Result<PaymentDto>.Failure(errors, StatusCodes.Status400BadRequest);
                }



                var Payment = await _context.Set<Payment>().FindAsync(request.Id);

                if (Payment == null)
                {
                    throw new BadRequestException(ValidatorTransform.NotExistsValue(Modules.Payment.Module,
                                    request.Id.ToString()));
                }

                Payment.CopyPropertiesFrom(request);




                var newEntity = _context.Set<Payment>().Update(Payment);
                await _context.SaveChangesAsync();

                var PaymentDto = _mapper.Map<PaymentDto>(newEntity.Entity);

                return Result<PaymentDto>.Success(PaymentDto, StatusCodes.Status200OK);

            }
            catch (Exception ex)
            {
                // Trả về lỗi nếu có exception
                return Result<PaymentDto>.Failure($"Đã có lỗi xảy ra: {ex.Message}", StatusCodes.Status500InternalServerError);
            }
        }





        

    }
}
