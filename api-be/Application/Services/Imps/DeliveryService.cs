using api_be.Domain.Models.Request.DeliveryRequest;
using api_be.Domain.Models.Responses;
using api_be.Domain.DefaultValidatorBase;
using api_be.Application.ValidatorRequest.DeliveryValidator;
using Microsoft.EntityFrameworkCore;
using static api_be.Core.Entities.Delivery;
using System.Threading;
using api_be.Core.Domain.Interfaces;
using Sieve.Services;
using api_be.Domain.Extensions;
using api_be.Core.Entities;
using api_be.Application.ValidatorRequest.DeliveryValidator.BaseDelivery;
using api_be.Domain.Exceptions;
using api_be.Domain.Transforms;
using Sieve.Models;
using FluentValidation;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using api_be.Infrastructure.DB;
using api_be.Infrastructure.Services;
using Azure.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using AutoMapper;

namespace api_be.Application.Services.Imps
{
    public class DeliveryService : IDeliveryService
    {
        private readonly ISupermarketDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IMapper _mapper;
        private readonly ISieveProcessor _sieveProcessor;
        private readonly ICurrentUserService _currentUserService;


        public DeliveryService(ISupermarketDbContext pContext, IConfiguration pConfiguration, IMapper pMapper,
            ISieveProcessor pSieveProcessor,ICurrentUserService currentUserService)
        {
            _context = pContext;
            _configuration = pConfiguration;
            _mapper = pMapper;
            _sieveProcessor = pSieveProcessor;
            _currentUserService = currentUserService;

        }
        public async Task HandleAfterChangeStatusDelivery(ChangeStatusDeliveryRequest request,  int? staffId)
        {
            if (request.Status == DeliveryStatus.Transport)
            {
                // Cập nhật nhân viên giao hàng và thời gian giao hàng
                var delivery = await _context.Deliveries
                    .FindAsync(request.DeliveryId);

                delivery.DateSent = DateTime.Now;
                delivery.ShipperId = staffId;

                _context.Set<Delivery>().Update(delivery);
                await _context.SaveChangesAsync();

            }
            else if (request.Status == DeliveryStatus.Delivered)
            {
                // Cập nhật thời gian nhận hàng
                var delivery = await _context.Set<Delivery>()
                    .FindAsync(request.DeliveryId);
                if (delivery != null)
                delivery.DateReceipt = DateTime.Now;

                _context.Deliveries.Update(delivery);
                await _context.SaveChangesAsync();
            }

            await Task.CompletedTask;
        }

        public async Task<Result<bool>> ChangeStatus(ChangeStatusDeliveryRequest request)
        {
            try
            {
                var validator = new ChangeStatusDeliveryValidator(_context);
                var validationResult = await validator.ValidateAsync(request);

                if (validationResult.IsValid == false)
                {
                    var errorMessages = validationResult.Errors.Select(x => x.ErrorMessage).ToList();
                    return Result<bool>.Failure(errorMessages, StatusCodes.Status400BadRequest);
                }

                var delivery = await _context.Deliveries.FindAsync(request.DeliveryId);

                DeliveryStatus? oldStatus = delivery.Status;

                delivery.Status = request.Status;
                delivery.PackingStaffId = _currentUserService.StaffId;

                _context.Deliveries.Update(delivery);
                await _context.SaveChangesAsync();

                // Sự kiện sau khi xác nhận đơn hàng
                await HandleAfterChangeStatusDelivery(request, _currentUserService.StaffId);

                return Result<bool>.Success(true, StatusCodes.Status200OK);
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure(ex.Message, StatusCodes.Status500InternalServerError);
            }
        }

        public async Task<Result<DeliveryDto>> Create(CreateOrUpdateDeliveryRequest request)
        {
            try
            {
                var validator = new BaseDeliveryValidator(_context);
                var validationResult = await validator.ValidateAsync(request);

                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                    return Result<DeliveryDto>.Failure(errors, StatusCodes.Status400BadRequest);
                }

                var delivery = _mapper.Map<Delivery>(request);

                var createDate = DateTime.Now;
                delivery.InternalCode = CommonService.InternalCodeGeneration("ORDER", createDate);
                delivery.DateSent = createDate;
                delivery.Status = DeliveryStatus.Prepare;
                delivery.PackingStaffId = _currentUserService.StaffId;

                var newDelivery = await _context.Set<Delivery>().AddAsync(delivery);
                await _context.SaveChangesAsync();

                var deliveryDto = _mapper.Map<DeliveryDto>(newDelivery.Entity);

                return Result<DeliveryDto>.Success(deliveryDto, StatusCodes.Status201Created);
            }
            catch (Exception ex)
            {
                return Result<DeliveryDto>.Failure($"An error occurred: {ex.Message}", StatusCodes.Status500InternalServerError);
            }
        }

        public async Task<Result<bool>> Delete(int id)
        {
            try
            {

                if (id == null || id <= 0)
                {
                    return Result<bool>.Failure(ValidatorTransform.Required(Modules.Delivery.Module), StatusCodes.Status400BadRequest);

                }

                var Delivery = await _context.Set<Delivery>().FindAsync(id);
                if (Delivery == null)
                {
                    throw new NotFoundException(Modules.Delivery.Id, id.ToString());
                }

                _context.Set<Delivery>().Remove(Delivery);
                await _context.SaveChangesAsync();
                return Result<bool>.Success(true, StatusCodes.Status200OK);
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure($"An error occurred: {ex.Message}", StatusCodes.Status500InternalServerError);
            }
        }

        public async Task<Result<DeliveryDto>> Detail(DetailBaseCommand request)
        {
            try
            {
                // Validate ID
                var validator = new DetailBaseValidator(_context);
                var validationResult = await validator.ValidateAsync(request);

                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                    return Result<DeliveryDto>.Failure(errors, StatusCodes.Status400BadRequest);
                }


                var delivery = _context.Set<Delivery>().FilterDeleted().Where(x => x.Id == request.Id);
                delivery = delivery.Include(x => x.Shipper).Include(x => x.PackingStaff);

                var findEntity = await delivery.SingleOrDefaultAsync();

                if (findEntity is null)
                {
                    return Result<DeliveryDto>.Failure(ValidatorTransform.NotExistsValue(Modules.Id, request.Id.ToString()),
                     StatusCodes.Status404NotFound);
                }




                // Map to DTO

                var DeliveryDto = _mapper.Map<DeliveryDto>(findEntity);


                return Result<DeliveryDto>.Success(DeliveryDto, StatusCodes.Status200OK);
            }
            catch (Exception ex)
            {
                return Result<DeliveryDto>.Failure($"An error occurred: {ex.Message}", StatusCodes.Status500InternalServerError);
            }
        }

        public async Task<PaginatedResult<List<DeliveryDto>>> GetList(ListBaseCommand request)
        {
            try
            {
                var validator = new ListBaseCommandValidator(_context);
                var validationResult = await validator.ValidateAsync(request);

                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                    return PaginatedResult<List<DeliveryDto>>.Failure(StatusCodes.Status400BadRequest, errors);

                }
                var query = _context.Set<Delivery>().FilterDeleted();

                if (request.IsAllDetail)
                {
                    query = query.Include(x => x.PackingStaff);
                    query = query.Include(x => x.Shipper);
                }





                var sieveModel = _mapper.Map<SieveModel>(request);




                var totalCount = await PaginatedResultBase.CountApplySieveAsync(_sieveProcessor, sieveModel, query);


                var paginatedQuery = _sieveProcessor.Apply(sieveModel, query);

                var Deliverys = await paginatedQuery.ToListAsync();



                var DeliveryDtos = _mapper.Map<List<DeliveryDto>>(Deliverys);
                var paginatedResult = PaginatedResult<List<DeliveryDto>>.Create(DeliveryDtos, totalCount, request.Page.Value, request.PageSize.Value, StatusCodes.Status200OK);

                return paginatedResult;
            }
            catch (Exception ex)
            {
                return PaginatedResult<List<DeliveryDto>>.Failure(StatusCodes.Status500InternalServerError, new List<string> { ex.Message });
            }
        }

        public async Task<Result<DeliveryDto>> Update(CreateOrUpdateDeliveryRequest request)
        {
            try
            {
                var validator = new BaseDeliveryValidator(_context);
                var validationResult = await validator.ValidateAsync(request);
                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                    return Result<DeliveryDto>.Failure(errors, StatusCodes.Status400BadRequest);
                }



                var delivery = await _context.Set<Delivery>().FindAsync(request.Id);


                if (delivery == null)
                {
                    throw new BadRequestException(ValidatorTransform.NotExistsValue(Modules.Delivery.Module,
                                    request.Id.ToString()));
                }

                delivery.DateSent = DateTime.Now;
                delivery.PackingStaffId = _currentUserService.StaffId;


                delivery.CopyPropertiesFrom(request);




                var newEntity = _context.Set<Delivery>().Update(delivery);
                await _context.SaveChangesAsync();

                var deliveryDto = _mapper.Map<DeliveryDto>(newEntity.Entity);

                return (Result<DeliveryDto>.Success(deliveryDto, StatusCodes.Status200OK));

            }
            catch (Exception ex)
            {
                // Trả về lỗi nếu có exception
                return Result<DeliveryDto>.Failure($"Đã có lỗi xảy ra: {ex.Message}", StatusCodes.Status500InternalServerError);
            }
        }
    }
}
