using api_be.Application.Models.Request.ImportGoodRequest;
using api_be.Application.Models.ValidatorRequest.DefaultValidatorBase;
using api_be.Application.Models.ValidatorRequest.ImportGoodsValidator;
using api_be.Application.Responses;
using api_be.Core.Domain.Interfaces;
using api_be.Domain.Exceptions;
using api_be.Domain.Extensions;
using api_be.Domain.ResultResponses;
using api_be.Domain.Transforms;
using api_be.Infrastructure.DB;
using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Sieve.Models;
using Sieve.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using api_be.Core.Entities;
using Microsoft.EntityFrameworkCore;
using api_be.Infrastructure.Services;
using static api_be.Core.Entities.SupplierOrder;
using System.Threading;
using api_be.Middleware;
using Microsoft.Extensions.DependencyInjection;

namespace api_be.Application.Services.Imps
{
    [RegisterService(ServiceLifetime.Scoped)]

    public class ImportGoodsService : IImportGoodsService
    {
        private readonly ISupermarketDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IMapper _mapper;
        private readonly ISieveProcessor _sieveProcessor;
        private readonly ICurrentUserService _currentUserService;
        public ImportGoodsService(ISupermarketDbContext pContext, IConfiguration pConfiguration, IMapper pMapper, ISieveProcessor pSieveProcessor, ICurrentUserService currentUserService)
        {
            _context = pContext;
            _configuration = pConfiguration;
            _mapper = pMapper;
            _sieveProcessor = pSieveProcessor;
            _currentUserService = currentUserService;
        }
        private async Task HandleAfterChangeStatusImportGoods(ChangeStatusImportGoodsRequest request)
        {
            if (request?.IsCancel != true)
            {
                var details = await _context.DetailSupplierOrders
                    .Where(x => x.SupplierOrderId == request.SupplierOrderId)
                    .ToListAsync();

                for (int i = 0; i < details.LongCount(); i++)
                {
                    var product = await _context.Products.FindAsync(details[i].ProductId);
                    product.Quantity += details[i].Quantity;
                    _context.Products.Update(product);
                    await _context.SaveChangesAsync(default(CancellationToken));

                    var price = await _context.DetailSupplierOrders
                        .Where(x => x.SupplierOrderId == request.SupplierOrderId &&
                                    x.ProductId == details[i].ProductId)
                        .Select(x => x.Price)
                        .FirstOrDefaultAsync();

                    var newProduct = new Product();
                    newProduct.CopyPropertiesFrom(product);
                    newProduct.Id = 0;
                    newProduct.ParentId = product.Id;
                    newProduct.Type = Product.ProductType.Single;
                    newProduct.Price = price;
                    newProduct.Quantity = details[i].Quantity;

                    await _context.Products.AddAsync(newProduct);
                    await _context.SaveChangesAsync();
                }
            }

            await Task.CompletedTask;
        }

        public async Task<Result<bool>> ChangeStatus(ChangeStatusImportGoodsRequest request)
        {

            try
            {
                var validator = new ChangeStatusImportGoodsValidator(_context);
                var validationResult = await validator.ValidateAsync(request);

                if (validationResult.IsValid == false)
                {
                    var errorMessages = validationResult.Errors.Select(x => x.ErrorMessage).ToList();
                    return Result<bool>.Failure(errorMessages, StatusCodes.Status400BadRequest);
                }

                var order = await _context.SupplierOrders.FindAsync(request.SupplierOrderId);

                if (request.IsCancel == true)
                {
                    order.Status = SupplierOrderStatus.Cancel;
                }
                else
                {
                    order.Status = SupplierOrderStatus.Completed;

                    var child = await _context.SupplierOrders
                        .FindAsync(request.SupplierOrderId);
                    var supplierOrder = await _context.SupplierOrders
                        .FindAsync(child.ParentId);

                    // Nếu đã nhập hết thì sửa parent thành Completed; PartialReceipt
                    var details = await _context.DetailSupplierOrders
                        .Where(x => x.SupplierOrderId == supplierOrder.Id)
                        .ToListAsync();
                    bool flag = false;
                    foreach (var item in details)
                    {
                        // Kiểm tra nhập hết chưa
                        var countProduct = await _context.DetailSupplierOrders
                            .Include(x => x.SupplierOrder)
                            .Where(x => x.SupplierOrder.ParentId == supplierOrder.Id &&
                                        x.ProductId == item.ProductId)
                            .SumAsync(x => x.Quantity);
                        if (countProduct < item.Quantity)
                        {
                            supplierOrder.Status = SupplierOrderStatus.PartialReceipt;
                            flag = true;
                            break;
                        }
                    }
                    if (!flag)
                    {
                        supplierOrder.Status = SupplierOrderStatus.Completed;
                    }
                    _context.SupplierOrders.Update(supplierOrder);
                    await _context.SaveChangesAsync();
                }
                order.ApproveStaffId = _currentUserService.StaffId;

                _context.SupplierOrders.Update(order);
                await _context.SaveChangesAsync();

                // Xác nhận nhập hàng: Thêm sự kiện cập nhật lại sản phẩm
                await  HandleAfterChangeStatusImportGoods(request);

                return Result<bool>.Success(true, StatusCodes.Status200OK);
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure(ex.Message, StatusCodes.Status500InternalServerError);
            }
        }
        private async Task HandleAfterCreateImportGoodUpdateImportGoodEvent(CreateImportGoodsRequest request, SupplierOrder So)
        {
            var supplierOrder = await _context.SupplierOrders.FindAsync(So.Id);
            supplierOrder.ParentId = request?.SupplierOrderId;
            _context.SupplierOrders.Update(supplierOrder);
            await _context.SaveChangesAsync(default(CancellationToken));

            await Task.CompletedTask;
        }
        private async Task HandleAfterCreateImportGoods(CreateImportGoodsRequest request, SupplierOrder So)
        {
            for (int i = 0; i < request?.Details?.LongCount(); i++)
            {
                var price = await _context.DetailSupplierOrders
                    .Where(x => x.SupplierOrderId == request.SupplierOrderId &&
                                x.ProductId == request.Details[i].ProductId)
                    .Select(x => x.Price)
                    .FirstOrDefaultAsync();

                var detail = new DetailSupplierOrder
                {
                    SupplierOrderId = So.Id,
                    ProductId = request.Details[i].ProductId,
                    Price = price,
                    Quantity = request.Details[i].ImportQuantity
                };
                await _context.DetailSupplierOrders.AddAsync(detail);
            }
            await _context.SaveChangesAsync();
            await Task.CompletedTask;
        }

        public async Task<Result<ImportGoodDto>> Create(CreateImportGoodsRequest request)
        {
            try
            {
                var validator = new CreateImportGoodsValidator(_context,request.SupplierOrderId);
                var validationResult = await validator.ValidateAsync(request);

                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                    return Result<ImportGoodDto>.Failure(errors, StatusCodes.Status400BadRequest);
                }

                var supplierOrder = await _context.SupplierOrders
                        .Include(x => x.Distributor)
                        .FirstOrDefaultAsync(x => x.Id == request.SupplierOrderId);

                var ImportGood = new SupplierOrder();
                ImportGood.Id = 0;
                ImportGood.CopyPropertiesFrom(supplierOrder);

                var productIds = request.Details.Select(d => d.ProductId).ToList();

                var detailsTotalPrice = await _context.DetailSupplierOrders
                    .Where(x => x.SupplierOrderId == request.SupplierOrderId &&
                                productIds.Contains(x.ProductId))
                    .ToListAsync();

                var totalPrice = detailsTotalPrice
                    .Sum(detail => detail.Price * request.Details
                    .FirstOrDefault(d => d.ProductId == detail.ProductId)?.ImportQuantity ?? 0);

                DateTime created = DateTime.Now;
                ImportGood.InternalCode = CommonService.InternalCodeGeneration("IMP_GOOD_", created);
                ImportGood.BookingDate = created;
                ImportGood.Total = totalPrice;
                ImportGood.Type = SupplierOrder.SupplierOrderType.Receive;
                ImportGood.Status = SupplierOrder.SupplierOrderStatus.Draft;
                ImportGood.ApproveStaffId = _currentUserService.StaffId;
                ImportGood.ReceivingStaff = request.ReceivingStaff;


                //var ImportGood = _mapper.Map<SupplierOrder>(request);
                var newImportGood = await _context.Set<SupplierOrder>().AddAsync(ImportGood);
                await _context.SaveChangesAsync();

                var ImportGoodDto = _mapper.Map<ImportGoodDto>(newImportGood.Entity);

              await  HandleAfterCreateImportGoods(request, newImportGood.Entity) ;
               await ChangeStatus(new ChangeStatusImportGoodsRequest
                {
                    SupplierOrderId = newImportGood.Entity.Id,
                    IsCancel = false
                });
                return Result<ImportGoodDto>.Success(ImportGoodDto, StatusCodes.Status201Created);
            }
            catch (Exception ex)
            {
                return Result<ImportGoodDto>.Failure($"An error occurred: {ex.Message}", StatusCodes.Status500InternalServerError);
            }
        }

        public async Task<Result<bool>> Delete(int id)
        {
            try
            {

                if (id == null || id <= 0)
                {
                    return Result<bool>.Failure(ValidatorTransform.Required(Modules.SupplierOrder.Module), StatusCodes.Status400BadRequest);
                    //throw new BadRequestException(string.Join(",", ex.Message));

                }

                var ImportGood = await _context.Set<SupplierOrder>().FindAsync(id);
                if (ImportGood == null)
                {
                    throw new NotFoundException(Modules.SupplierOrder.Module, id.ToString());
                }

                _context.Set<SupplierOrder>().Remove(ImportGood);
                await _context.SaveChangesAsync();
                return Result<bool>.Success(true, StatusCodes.Status200OK);
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure($"An error occurred: {ex.Message}", StatusCodes.Status500InternalServerError);
            }
        }

        public async Task<Result<ImportGoodDto>> Detail(DetailBaseCommand request)
        {
            try
            {
                // Validate ID
                var validator = new DetailBaseValidator(_context);
                var validationResult = await validator.ValidateAsync(request);

                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                    return Result<ImportGoodDto>.Failure(errors, StatusCodes.Status400BadRequest);
                }


                var ImportGood = _context.Set<SupplierOrder>().FilterDeleted().Where(x => x.Id == request.Id);
                ImportGood = ImportGood.Include(x => x.Distributor)
                  .Include(x => x.Parent)
                  .Include(x => x.ApproveStaff);

                var findEntity = await ImportGood.SingleOrDefaultAsync();

                if (findEntity is null)
                {
                    return Result<ImportGoodDto>.Failure(ValidatorTransform.NotExistsValue(Modules.Id, request.Id.ToString()),
                     StatusCodes.Status404NotFound);
                }




                // Map to DTO

                var ImportGoodDto = _mapper.Map<ImportGoodDto>(findEntity);

                var details = await _context.DetailSupplierOrders
                .Include(x => x.Product)
                .Where(x => x.SupplierOrderId == ImportGoodDto.Id)
                .Select(x => new ProductImportGoodDto
                {
                    Id = x.Product.Id,
                    InternalCode = x.Product.InternalCode,
                    Name = x.Product.Name,
                    Images = _mapper.Map<List<string>>(x.Product.Images),
                    Price = x.Product.Price,
                    Quantity = x.Quantity,
                    Describes = x.Product.Describes,
                    Feature = x.Product.Feature,
                    Specifications = x.Product.Specifications,
                    Status = x.Product.Status,
                    Category = new CategoryDto
                    {
                        Id = x.Product.Category.Id,
                        Name = x.Product.Category.Name
                    },
                    OrderQuantity = 0,
                    ImportQuantity = 0
                })
                .ToListAsync();
                foreach (var product in details)
                {
                    product.ImportQuantity = await _context.DetailSupplierOrders
                        .Include(x => x.SupplierOrder)
                        .Where(x => x.SupplierOrder.ParentId == ImportGoodDto.ParentId &&
                                    x.SupplierOrder.Status == SupplierOrderStatus.Completed &&
                                    x.SupplierOrder.Type == SupplierOrderType.Receive &&
                                    x.ProductId == product.Id)
                        .SumAsync(x => x.Quantity);

                    product.OrderQuantity = await _context.DetailSupplierOrders
                        .Include(x => x.SupplierOrder)
                        .Where(x => x.SupplierOrder.Id == ImportGoodDto.ParentId &&
                                    (x.SupplierOrder.Status == SupplierOrderStatus.Order ||
                                    x.SupplierOrder.Status == SupplierOrderStatus.PartialReceipt) &&
                                    x.SupplierOrder.Type == SupplierOrderType.Order &&
                                    x.ProductId == product.Id)
                        .SumAsync(x => x.Quantity);
                }

                ImportGoodDto.Details = _mapper.Map<List<ProductImportGoodDto>>(details);

                return Result<ImportGoodDto>.Success(ImportGoodDto, StatusCodes.Status200OK);
            }
            catch (Exception ex)
            {
                return Result<ImportGoodDto>.Failure($"An error occurred: {ex.Message}", StatusCodes.Status500InternalServerError);
            }
        }

        public async Task<PaginatedResult<List<ImportGoodDto>>> GetList(ListBaseCommand request)
        {
            try
            {
                var validator = new ListBaseCommandValidator(_context);
                var validationResult = await validator.ValidateAsync(request);

                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                    return PaginatedResult<List<ImportGoodDto>>.Failure(StatusCodes.Status400BadRequest, errors);

                }
                var query = _context.Set<SupplierOrder>().FilterDeleted();



                query = query.Where(x => x.Type == SupplierOrderType.Receive);

                if (request.IsAllDetail)
                {
                    query = query.Include(x => x.Distributor)
                                 .Include(x => x.Parent)
                                 .Include(x => x.ApproveStaff);
                }



                var sieveModel = _mapper.Map<SieveModel>(request);




                var totalCount = await PaginatedResultBase.CountApplySieveAsync(_sieveProcessor, sieveModel, query);


                var paginatedQuery = _sieveProcessor.Apply(sieveModel, query);

                var ImportGoods = await paginatedQuery.ToListAsync();



                var ImportGoodDtos = _mapper.Map<List<ImportGoodDto>>(ImportGoods);
                var paginatedResult = PaginatedResult<List<ImportGoodDto>>.Create(ImportGoodDtos, totalCount, request.Page.Value, request.PageSize.Value, StatusCodes.Status200OK);

                return paginatedResult;
            }
            catch (Exception ex)
            {
                return PaginatedResult<List<ImportGoodDto>>.Failure(StatusCodes.Status500InternalServerError, new List<string> { ex.Message });
            }
        }
        private async Task HandleAfterUpdateImportGoodsUpdateDetailSupplierOrderEvent( UpdateImportGoodsRequest request,SupplierOrder So)
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
                var price = await _context.DetailSupplierOrders
                    .Include(x => x.SupplierOrder)
                    .Where(x => x.SupplierOrder.ParentId ==request.Id &&
                                x.ProductId == request.Details[i].ProductId)
                    .Select(x => x.Price)
                    .FirstOrDefaultAsync();

                var detail = new DetailSupplierOrder
                {
                    SupplierOrderId = So.Id,
                    ProductId = create[i],
                    Price = price,
                    Quantity = request.Details[i].ImportQuantity
                };
                await _context.DetailSupplierOrders.AddAsync(detail);
            }

            for (int i = 0; i < update?.LongCount(); i++)
            {
                var price = await _context.DetailSupplierOrders
                    .Include(x => x.SupplierOrder)
                    .Where(x => x.SupplierOrder.ParentId == request.Id &&
                                x.ProductId == request.Details[i].ProductId)
                    .Select(x => x.Price)
                    .FirstOrDefaultAsync();

                var detail = await _context.DetailSupplierOrders
                    .Where(x => x.ProductId == update[i] &&
                                x.SupplierOrderId == So.Id)
                    .SingleOrDefaultAsync();
                detail.Price = price;
                detail.Quantity = request.Details[i].ImportQuantity;

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

            await _context.SaveChangesAsync();
            await Task.CompletedTask;
        }

        public async Task<Result<ImportGoodDto>> Update(UpdateImportGoodsRequest request)
        {
            try
            {
                var validator = new UpdateImportGoodsValidator(_context, null);
                var validationResult = await validator.ValidateAsync(request);
                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                    return Result<ImportGoodDto>.Failure(errors, StatusCodes.Status400BadRequest);
                }



                var ImportGood = await _context.Set<SupplierOrder>().FindAsync(request.Id);

                if (ImportGood == null)
                {
                    throw new BadRequestException(ValidatorTransform.NotExistsValue(Modules.Coupon.Module,
                                    request.Id.ToString()));
                }

                ImportGood.CopyPropertiesFrom(request);


                if (ImportGood.Status != SupplierOrder.SupplierOrderStatus.Draft)
                {
                    throw new BadRequestException("Đơn hàng đã được đặt không được thay đổi!");
                }

                ImportGood.BookingDate = DateTime.Now;

                var productIds = request.Details.Select(d => d.ProductId).ToList();

                var detailsTotalPrice = await _context.DetailSupplierOrders
                    .Include(x => x.SupplierOrder)
                    .Where(x => x.SupplierOrder.Id == request.Id &&
                                productIds.Contains(x.ProductId))
                    .ToListAsync();

                var totalPrice = detailsTotalPrice
                    .Sum(detail => detail.Price * request.Details
                    .FirstOrDefault(d => d.ProductId == detail.ProductId)?.ImportQuantity ?? 0);

                ImportGood.Total = totalPrice;
                ImportGood.ReceivingStaff = request.ReceivingStaff;
                ImportGood.ApproveStaffId = _currentUserService.StaffId;


                var newEntity = _context.Set<SupplierOrder>().Update(ImportGood);
                await _context.SaveChangesAsync();

                var ImportGoodDto = _mapper.Map<ImportGoodDto>(newEntity.Entity);
                await HandleAfterUpdateImportGoodsUpdateDetailSupplierOrderEvent(request, newEntity.Entity);


                return (Result<ImportGoodDto>.Success(ImportGoodDto, StatusCodes.Status200OK));

            }
            catch (Exception ex)
            {
                // Trả về lỗi nếu có exception
                return Result<ImportGoodDto>.Failure($"Đã có lỗi xảy ra: {ex.Message}", StatusCodes.Status500InternalServerError);
            }
        }
    }
}
