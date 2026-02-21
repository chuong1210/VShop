using api_be.Application.Models.Request.ImportGoodRequest;
using api_be.Application.Models.Request.SupplierOrderRequest;
using api_be.Application.Models.ValidatorRequest.DefaultValidatorBase;
using api_be.Application.Models.ValidatorRequest.ImportGoodsValidator;
using api_be.Application.Models.ValidatorRequest.SupllierOrderValidator;
using api_be.Application.Responses;
using api_be.Core.Domain.Interfaces;
using api_be.Core.Entities;
using api_be.Domain.Exceptions;
using api_be.Domain.Extensions;
using api_be.Domain.ResultResponses;
using api_be.Domain.Transforms;
using api_be.Infrastructure.DB;
using api_be.Infrastructure.Services;
using api_be.Middleware;
using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sieve.Models;
using Sieve.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static api_be.Core.Entities.SupplierOrder;

namespace api_be.Application.Services.Imps
{
    [RegisterService(ServiceLifetime.Scoped)]

    public class SupplierOrderService : ISupplierOrderService
    {
        private readonly ISupermarketDbContext _context;
        private readonly IMapper _mapper;
        private readonly ISieveProcessor _sieveProcessor;
        private readonly ICurrentUserService _currentUserService;
        private readonly IRedisInventoryService _redisInventoryService; // Inject RedisInventoryService

        public SupplierOrderService(
            ISupermarketDbContext context,
            IMapper mapper,
            ISieveProcessor sieveProcessor,
            ICurrentUserService currentUserService,
            IRedisInventoryService redisInventoryService)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _sieveProcessor = sieveProcessor ?? throw new ArgumentNullException(nameof(sieveProcessor));
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
            _redisInventoryService = redisInventoryService;
        }
        public async Task<Result<bool>> ChangeStatus(ChangeStatusSupplierOrderRequest request)
        {
            try
            {
                var validator = new ChangeStatusSupplierOrderValidator(_context);
                var validationResult = await validator.ValidateAsync(request);

                if (validationResult.IsValid == false)
                {
                    var errorMessages = validationResult.Errors.Select(x => x.ErrorMessage).ToList();
                    return Result<bool>.Failure(errorMessages, StatusCodes.Status400BadRequest);
                }

                var order = await _context.SupplierOrders.FindAsync(request.SupplierOrderId);

                order.Status = request.Status;
                order.ApproveStaffId = _currentUserService.StaffId;

                _context.SupplierOrders.Update(order);
                await _context.SaveChangesAsync();

                return Result<bool>.Success(true, StatusCodes.Status200OK);
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure(ex.Message, StatusCodes.Status500InternalServerError);
            }
        }

        private async Task HandleAfterCreateSupplierOrdeEvent(CreateOrUpdateSupplierOrderRequest request, SupplierOrder So)
        {
            for (int i = 0; i < request?.Details?.LongCount(); i++)
            {
                var detail = new DetailSupplierOrder
                {
                    SupplierOrderId = So.Id,
                    ProductId = request.Details[i].ProductId,
                    Price = request.Details[i].Price,
                    Quantity =request.Details[i].Quantity
                };
                await _context.DetailSupplierOrders.AddAsync(detail);
            }

            await _context.SaveChangesAsync(default(CancellationToken));
            await Task.CompletedTask;
        }
        public async Task<Result<SupplierOrderDto>> Create(CreateOrUpdateSupplierOrderRequest request)
        {
            try
            {
                var validator = new CreateOrUpdateSupplierOrderValidator(_context,null);
                var validationResult = await validator.ValidateAsync(request);

                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                    return Result<SupplierOrderDto>.Failure(errors, StatusCodes.Status400BadRequest);
                }
                var supplierOrder = _mapper.Map<SupplierOrder>(request);
                DateTime created = DateTime.Now;
                supplierOrder.InternalCode = CommonService.InternalCodeGeneration("SUP_ORDER_", created);
                supplierOrder.BookingDate = created;
                supplierOrder.Total = request?.Details?.Sum(x => x.Price * x.Quantity);
                supplierOrder.Type = SupplierOrder.SupplierOrderType.Order;
                supplierOrder.Status = SupplierOrder.SupplierOrderStatus.Draft;
                supplierOrder.ApproveStaffId = _currentUserService.StaffId;

                var newSupplierOrder = await _context.Set<SupplierOrder>().AddAsync(supplierOrder);
                await _context.SaveChangesAsync();

                var SupplierOrderDto = _mapper.Map<SupplierOrderDto>(newSupplierOrder.Entity);
                await HandleAfterCreateSupplierOrdeEvent(request, newSupplierOrder.Entity);



                return Result<SupplierOrderDto>.Success(SupplierOrderDto, StatusCodes.Status201Created);
            }

            catch (Exception ex)
            {
                return Result<SupplierOrderDto>.Failure($"An error occurred: {ex.Message}", StatusCodes.Status500InternalServerError);
            }
        }

        public async Task<Result<bool>> Delete(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return Result<bool>.Failure("Invalid SupplierOrder Id", StatusCodes.Status400BadRequest);
                }

                var importGood = await _context.SupplierOrders.FindAsync(id);
                if (importGood == null)
                {
                    return Result<bool>.Failure("SupplierOrder not found", StatusCodes.Status404NotFound);
                }

                // Truy vấn tất cả DetailSupplierOrders liên quan đến SupplierOrder
                var detailOrders = await _context.DetailSupplierOrders
                    .Where(d => d.SupplierOrderId == id)
                    .ToListAsync();

                if (detailOrders.Any())
                {
                    _context.DetailSupplierOrders.RemoveRange(detailOrders); // Xóa tất cả DetailSupplierOrders
                    await _context.SaveChangesAsync(); // Lưu thay đổi
                }

                // Sau khi xóa DetailSupplierOrders, tiếp tục xóa SupplierOrder
                _context.SupplierOrders.Remove(importGood);
                await _context.SaveChangesAsync();

                return Result<bool>.Success(true, StatusCodes.Status200OK);
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure($"An error occurred: {ex.Message}", StatusCodes.Status500InternalServerError);
            }
        }

        public async Task<Result<SupplierOrderDto>> Detail(DetailBaseCommand request)
        {
            try
            {
                // Validate ID
                var validator = new DetailBaseValidator(_context);
                var validationResult = await validator.ValidateAsync(request);

                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                    return Result<SupplierOrderDto>.Failure(errors, StatusCodes.Status400BadRequest);
                }


                var supplierOrder = _context.Set<SupplierOrder>().FilterDeleted().Where(x => x.Id == request.Id);
                supplierOrder = supplierOrder.Include(x => x.Distributor)
                 .Include(x => x.ApproveStaff);

                var findEntity = await supplierOrder.SingleOrDefaultAsync();

                if (findEntity is null)
                {
                    return Result<SupplierOrderDto>.Failure(ValidatorTransform.NotExistsValue(Modules.Id, request.Id.ToString()),
                     StatusCodes.Status404NotFound);
                }




                // Map to DTO

                var  SupplierOrderDto = _mapper.Map<SupplierOrderDto>(findEntity);

                var details = await _context.DetailSupplierOrders
               .Include(x => x.Product)
               .Where(x => x.SupplierOrderId == SupplierOrderDto.Id)
               .ToListAsync();

                SupplierOrderDto.Details = _mapper.Map<List<DetailSupplierOrderDto>>(details);


                return Result<SupplierOrderDto>.Success(SupplierOrderDto, StatusCodes.Status200OK);
            }
            catch (Exception ex)
            {
                return Result<SupplierOrderDto>.Failure($"An error occurred: {ex.Message}", StatusCodes.Status500InternalServerError);
            }
        }

        public async Task<PaginatedResult<List<SupplierOrderDto>>> GetList(ListBaseCommand request)
        {
            try
            {
                var validator = new ListBaseCommandValidator(_context);
                var validationResult = await validator.ValidateAsync(request);

                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                    return PaginatedResult<List<SupplierOrderDto>>.Failure(StatusCodes.Status400BadRequest, errors);

                }
                var query = _context.Set<SupplierOrder>().FilterDeleted();

                query = query.Where(x => x.Type == SupplierOrderType.Order);

                if (request.IsAllDetail)
                {
                    query = query.Include(x => x.Distributor)
                                 .Include(x => x.ApproveStaff);
                }


                var sieveModel = _mapper.Map<SieveModel>(request);




                var totalCount = await PaginatedResultBase.CountApplySieveAsync(_sieveProcessor, sieveModel, query);


                var paginatedQuery = _sieveProcessor.Apply(sieveModel, query);

                var supplierOrders = await paginatedQuery.ToListAsync();



                var supplierOrderDtos = _mapper.Map<List<SupplierOrderDto>>(supplierOrders);
                var paginatedResult = PaginatedResult<List<SupplierOrderDto>>.Create(supplierOrderDtos, totalCount, request.Page.Value, request.PageSize.Value, StatusCodes.Status200OK);

                return paginatedResult;
            }
            catch (Exception ex)
            {
                return PaginatedResult<List<SupplierOrderDto>>.Failure(StatusCodes.Status500InternalServerError, new List<string> { ex.Message });
            }
        }

        public async Task HandleAfterUpdateSupplierOrderEvent(CreateOrUpdateSupplierOrderRequest request,SupplierOrder So)
        {
            // Tìm sản phẩm chung và riêng
            var productIdDB = await _context.DetailSupplierOrders
                .Where(x => x.SupplierOrderId == request.Id)
                .Select(x => x.ProductId)
                .ToListAsync();
            var productIdRequest = request.Details
                .Select(x => x.ProductId)
                .ToList();

            // Thêm, sửa, xoá chi tiết
            var create = productIdRequest.Except(productIdDB).ToList();
            var update = productIdRequest.Intersect(productIdDB).ToList();
            var delete = productIdDB.Except(productIdRequest).ToList();

            for (int i = 0; i < create?.LongCount(); i++)
            {
                var detail = new DetailSupplierOrder
                {
                    SupplierOrderId = So.Id,
                    ProductId = create[i],
                    Price = request.Details[i].Price,
                    Quantity = request.Details[i].Quantity
                };
                await _context.DetailSupplierOrders.AddAsync(detail);
            }

            for (int i = 0; i < update?.LongCount(); i++)
            {
                var detail = await _context.DetailSupplierOrders
                    .Where(x => x.ProductId == update[i] &&
                                x.SupplierOrderId == So.Id)
                    .SingleOrDefaultAsync();
                detail.Price = request.Details[i].Price;
                detail.Quantity = request.Details[i].Quantity;

                _context.DetailSupplierOrders.Update(detail);
            }

            for (int i = 0; i < delete?.LongCount(); i++)
            {
                var detail = await _context.DetailSupplierOrders
                    .Where(x => x.ProductId == delete[i] &&
                                x.SupplierOrderId == So.Id)
                    .SingleOrDefaultAsync();
                _context.DetailSupplierOrders.Remove(detail);
            }

            await _context.SaveChangesAsync(default(CancellationToken));
            await Task.CompletedTask;
        }

        public async Task<Result<SupplierOrderDto>> Update(CreateOrUpdateSupplierOrderRequest request)
        {
            try
            {
                var validator = new CreateOrUpdateSupplierOrderValidator(_context, request.Id);
                var validationResult = await validator.ValidateAsync(request);
                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                    return Result<SupplierOrderDto>.Failure(errors, StatusCodes.Status400BadRequest);
                }



                var SupplierOrder = await _context.Set<SupplierOrder>().FindAsync(request.Id);

                if (SupplierOrder == null)
                {
                    throw new BadRequestException(ValidatorTransform.NotExistsValue(Modules.Coupon.Module,
                                    request.Id.ToString()));
                }

                SupplierOrder.CopyPropertiesFrom(request);

                if (SupplierOrder.Status != SupplierOrder.SupplierOrderStatus.Draft)
                {
                    throw new BadRequestException("Đơn hàng đã được đặt không được thay đổi!");
                }

                SupplierOrder.BookingDate = DateTime.Now;
                SupplierOrder.Total = request?.Details?.Sum(x => x.Price * x.Quantity);
                SupplierOrder.ApproveStaffId = _currentUserService.StaffId;




          

                var newEntity = _context.Set<SupplierOrder>().Update(SupplierOrder);
                await _context.SaveChangesAsync();

                var SupplierOrderDto = _mapper.Map<SupplierOrderDto>(newEntity.Entity);
                await HandleAfterUpdateSupplierOrderEvent(request, newEntity.Entity);


                return (Result<SupplierOrderDto>.Success(SupplierOrderDto, StatusCodes.Status200OK));

            }
            catch (Exception ex)
            {
                // Trả về lỗi nếu có SupplierOrderDto
                return Result<SupplierOrderDto>.Failure($"Đã có lỗi xảy ra: {ex.Message}", StatusCodes.Status500InternalServerError);
            }
        }

        public async Task<Result<List<ProductSupplierOrderDto>>> ProductSupplierOrder(DetailBaseCommand request)
        {
            try
            {
                var validator = new DetailBaseValidator(_context);
                var validationResult = await validator.ValidateAsync(request);

                if (!validationResult.IsValid)
                {
                    var errorMessages = validationResult.Errors.Select(x => x.ErrorMessage).ToList();
                    return PaginatedResult<List<ProductSupplierOrderDto>>.Failure(StatusCodes.Status400BadRequest, errorMessages);
                }
                var products = await _context.DetailSupplierOrders
                        .Where(x => x.SupplierOrderId == request.Id)
                        .Include(x => x.Product)
                        .Include(x => x.Product.Category)
                        .Select(x => new ProductSupplierOrderDto
                        {
                            Id = x.Product.Id,
                            InternalCode = x.Product.InternalCode,
                            Name = x.Product.Name,
                            Images = _mapper.Map<List<string>>(x.Product.Images),
                            Price = x.Product.Price,
                            Quantity = x.Product.Quantity,
                            Describes = x.Product.Describes,
                            Feature = x.Product.Feature,
                            Specifications = x.Product.Specifications,
                            Status = x.Product.Status,
                            Category = new CategoryDto
                            {
                                Id = x.Product.Category.Id,
                                Name = x.Product.Category.Name
                            },
                            OrderQuantity = x.Quantity,
                            ImportQuantity = 0
                        })
                        .ToListAsync();
                foreach (var product in products)
                {
                    product.ImportQuantity = await _context.DetailSupplierOrders
                        .Include(x => x.SupplierOrder)
                        .Where(x => x.SupplierOrder.ParentId == request.Id &&
                                    x.SupplierOrder.Status == SupplierOrderStatus.Completed &&
                                    x.SupplierOrder.Type == SupplierOrderType.Receive &&
                                    x.ProductId == product.Id)
                        .SumAsync(x => x.Quantity);
                }

                var mapResults = _mapper.Map<List<ProductSupplierOrderDto>>(products);

                return Result<List<ProductSupplierOrderDto>>.Success(mapResults, StatusCodes.Status200OK);

            }
            catch (Exception ex)
            {
                return Result<List<ProductSupplierOrderDto>>.Failure(ex.Message, StatusCodes.Status500InternalServerError);
            }
        }
    }
}
