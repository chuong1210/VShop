using api_be.Domain.Constants;
using api_be.Core.Entities;
using api_be.Middleware;
using api_be.Domain.Models.Request.OrderRequest;
using api_be.Domain.Models.Responses;
using api_be.Domain.DefaultValidatorBase;
using api_be.Application.ValidatorRequest.OrderValidator.BaseOrders;
using api_be.Application.ValidatorRequest.OrderValidator;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using api_be.Core.Domain.Interfaces;
using Sieve.Services;
using api_be.Domain.Extensions;
using static api_be.Core.Entities.Order;
using api_be.Application.Services;
using api_be.Infrastructure.Services;
using api_be.Domain.Transforms;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using Sieve.Models;
using api_be.Infrastructure.DB;
using Microsoft.Extensions.DependencyInjection;
using AutoMapper;
using Microsoft.AspNetCore.Http;

namespace api_be.Application.Services.Imps
{
    [RegisterService(ServiceLifetime.Scoped)]
    public class OrderService : IOrderService
    {
            
        private readonly ISupermarketDbContext _context;
        private readonly IMapper _mapper;
        private readonly ISieveProcessor _sieveProcessor;
        private readonly ICurrentUserService _currentUserService;
        private readonly IRedisInventoryService _redisInventoryService; // Inject RedisInventoryService

        public OrderService(
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
        public async Task<Result<bool>> AddCouponToCart(AddCouponToCartRequest request)
        {
            try
            {
                var validator = new AddCouponToCartValidator(_context, _currentUserService.CustomerId);
                var validationResult = await validator.ValidateAsync(request);

                if (validationResult.IsValid == false)
                {
                    var errorMessages = validationResult.Errors.Select(x => x.ErrorMessage).ToList();
                    return Result<bool>.Failure(errorMessages, StatusCodes.Status400BadRequest);
                }

                // Kiểm tra nếu không phải người dùng thì không có quyền
                if (_currentUserService.Type != CLAIMS_VALUES.TYPE_USER)
                {
                    return Result<bool>.Failure("Vui lòng đăng ký tài khoản người dùng để đặt hàng!", StatusCodes.Status403Forbidden);
                }

                var cart = await _context.Orders
                    .Where(x => x.CustomerId == _currentUserService.CustomerId &&
                    x.Status == Order.OrderStatus.Cart)
                    .SingleOrDefaultAsync();
                var coupon = await _context.Coupons
                    .Where(x => x.InternalCode == request.InternalCodeCoupon)
                    .SingleOrDefaultAsync();

                // Tính toán tiền khuyến mãi trên đơn hàng
                decimal? priceDiscout = 0;
                if (cart == null)
                {
                    return Result<bool>.Success(false, StatusCodes.Status400BadRequest);
                }
                else
                {
                    if (coupon.Type == Coupon.CouponType.Percent)
                    {
                        priceDiscout = cart.Total * (coupon.Percent * 0.01m) > coupon.DiscountMax ?
                                            coupon.DiscountMax : cart.Total * (coupon.Percent * 0.01m);
                    }
                    else if (coupon.Type == Coupon.CouponType.Discount)
                    {
                        priceDiscout = coupon.Discount > cart.Total * (coupon.PercentMax * 0.01m) ?
                                            cart.Total * (coupon.PercentMax * 0.01m) : coupon.Discount;
                    }
                }

                cart.CouponId = coupon.Id;
                cart.TotalDecrease = priceDiscout;
                cart.TotalAmount = cart.Total - priceDiscout;
                _context.Orders.Update(cart);
                await _context.SaveChangesAsync();

                return Result<bool>.Success(true, StatusCodes.Status200OK);
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure(ex.Message, StatusCodes.Status500InternalServerError);
            }
        }


        private async Task HandleBeforeAddProductToCart(int? userId)
        {
            var cart = await _context.Orders
                .Include(x => x.Customer).ThenInclude(x => x.User)
                .Where(x => x.Customer.User.Id == userId &&
                            x.Status == Order.OrderStatus.Cart)
                .FirstOrDefaultAsync();

            if (cart == null)
            {
                var user = await _context.Users.FindAsync(userId);

                // Tạo mới giỏ hàng
                var order = new Order
                {
                    CustomerId = user.CustomerId,
                    TotalAmount = 0,
                    TotalDecrease = 0,
                    Total = 0,
                    Status = Order.OrderStatus.Cart,
                    Type = Order.OrderType.Online,
                };

                await _context.Orders.AddAsync(order);
                await _context.SaveChangesAsync();
            }
        }
        private async Task HandleAfterAddProductToCart(int? CustomerId)
        {

            // Lấy sản phẩm trong giỏ hàng được chọn ra
            var detail = await _context.DetailOrders
                        .Include(x => x.Order)
                        .Where(x => x.Order.CustomerId == CustomerId &&
                                    x.Order.Status == Order.OrderStatus.Cart)
                        .ToListAsync();
            var productsId = detail.Select(x => x.ProductId).ToList();

            // Lấy tất cả CTKM theo nhóm ra
            var groupsG = await _context.PromotionProductRequirements
                .Where(x => x.Promotion.Start <= DateTime.Now &&
                            DateTime.Now <= x.Promotion.End &&
                            x.Promotion.Limit >= 1 &&
                            x.Promotion.Status == Promotion.PromotionStatus.Approve)
                .GroupBy(x => x.Group)
                .ToListAsync();

            var uniqueGroups = groupsG.Where(group => group.Count() > 1).ToList();
            var groups = uniqueGroups.Where(x => x.All(g => productsId.Contains(g.ProductId))).ToList();

            if (groups.Count() == 0)
            {
                return;
            }
            List<int?> uniqueGroup = new List<int?>();
            if (groups.Count() == 1)
            {
                uniqueGroup.Add(groups[0].Key);
            }
            else
            {
                for (int i = 0; i < groups.Count - 1; i++)
                {
                    for (int j = i + 1; j < groups.Count; j++)
                    {
                        var productI = groups[i].Select(x => x.ProductId).ToList();
                        var productJ = groups[j].Select(x => x.ProductId).ToList();
                        bool haveCommonProduct = productI.Intersect(productJ).Any();
                        if (haveCommonProduct)
                        {
                            var promotionI = await _context.PromotionProductRequirements
                                .Include(x => x.Promotion)
                                .Where(x => x.Group == groups[i].Key)
                                .Select(x => x.Promotion)
                                .FirstOrDefaultAsync();

                            var promotionJ = await _context.PromotionProductRequirements
                                .Include(x => x.Promotion)
                                .Where(x => x.Group == groups[j].Key)
                                .Select(x => x.Promotion)
                                .FirstOrDefaultAsync();

                            // Nhiều sp hơn chưa chắc km hơn
                            int? groupAdd = productI.Count > productJ.Count ?
                                                    groups[i].Key : groups[j].Key;
                            uniqueGroup.Add(groupAdd);
                        }
                    }
                }
            }

            foreach (var group in uniqueGroup)
            {
                var promotionDetail = await _context.PromotionProductRequirements
                    .Include(x => x.Promotion)
                    .Include(x => x.Product)
                    .Where(x => x.Group == group)
                    .ToListAsync();
                decimal? number = promotionDetail.Count();
                foreach (var item in promotionDetail)
                {
                    var detailOrder = detail
                            .Where(x => x.ProductId == item.Product.Id)
                            .SingleOrDefault();
                    if (item.Promotion.Type == Promotion.PromotionType.Percent)
                    {
                        decimal? priceDiscout = item.Product.Price * (item.Promotion.Percent * 0.01m) > item.Promotion.DiscountMax ?
                                        item.Promotion.DiscountMax : item.Product.Price * (item.Promotion.Percent * 0.01m);
                        detailOrder.ReducedPrice = priceDiscout;
                        detailOrder.Price = item.Product.Price - priceDiscout;
                        detailOrder.GroupPromotion = group;
                    }
                    else if (item.Promotion.Type == Promotion.PromotionType.Discount)
                    {
                        decimal? priceDiscout = (item.Promotion.Discount / number) > item.Product.Price * (item.Promotion.PercentMax * 0.01m) ?
                                        item.Product.Price * (item.Promotion.PercentMax * 0.01m) : (item.Promotion.Discount / number);
                        detailOrder.ReducedPrice = priceDiscout;
                        detailOrder.Price = item.Product.Price - priceDiscout;
                        detailOrder.GroupPromotion = group;
                    }
                    _context.DetailOrders.Update(detailOrder);
                    await _context.SaveChangesAsync();
                }
            }

            await Task.CompletedTask;
        }


        public async Task HandleAfterUpdateProductToCart(int cartId)
        {
            var cart = await _context.Orders.FindAsync(cartId);

            // Tính tiền trên đơn hàng
            var details = await _context.DetailOrders
                .Where(x => x.OrderId == cart.Id && x.IsSelected == true)
                .ToListAsync();
            cart.TotalAmount = details.Sum(x => x.Price * x.Quantity ?? 0);
            cart.TotalDecrease = 0;
            cart.Total = details.Sum(x => x.Price * x.Quantity ?? 0);

            // Tính khuyến mãi trên đơn hàng
            var coupon = await _context.Coupons.FindAsync(cart.CouponId);

            if (coupon != null)
            {
                decimal? priceDiscout = 0;
                if (coupon.Type == Coupon.CouponType.Percent)
                {
                    priceDiscout = cart.Total * (coupon.Percent * 0.01m) > coupon.DiscountMax ?
                                        coupon.DiscountMax : cart.Total * (coupon.Percent * 0.01m);
                }
                else if (coupon.Type == Coupon.CouponType.Discount)
                {
                    priceDiscout = coupon.Discount > cart.Total * (coupon.PercentMax * 0.01m) ?
                                        cart.Total * (coupon.PercentMax * 0.01m) : coupon.Discount;
                }

                cart.TotalDecrease = priceDiscout;
                cart.TotalAmount = cart.Total - priceDiscout;
            }

            _context.Orders.Update(cart);
            await _context.SaveChangesAsync();

            await Task.CompletedTask;
        }
    
    public async Task<Result<bool>> AddProductToCart(AddProductToCartRequest request)
        {
            try
            {
                var validator = new AddProductToCartValidator(_context);
                var validationResult = await validator.ValidateAsync(request);

                if (validationResult.IsValid == false)
                {
                    var errorMessages = validationResult.Errors.Select(x => x.ErrorMessage).ToList();
                    return Result<bool>.Failure(errorMessages, StatusCodes.Status400BadRequest);
                }

                // Kiểm tra nếu không phải người dùng thì không có quyền
                if (_currentUserService.Type != CLAIMS_VALUES.TYPE_USER)
                {
                    return Result<bool>.Failure("Vui lòng đăng ký tài khoản người dùng để đặt hàng!", StatusCodes.Status403Forbidden);
                }

                await HandleBeforeAddProductToCart(_currentUserService.UserId);


                // Lấy giỏ hàng của người dùng
                var cart = await _context.Orders
                    .Include(x => x.Customer).ThenInclude(x => x.User)
                    .Where(x => x.Customer.User.Id == _currentUserService.UserId &&
                                x.Status == Order.OrderStatus.Cart)
                    .FirstOrDefaultAsync();

                var detail = await _context.DetailOrders
                            .Where(x => x.ProductId == request.ProductId &&
                                        x.OrderId == cart.Id)
                            .FirstOrDefaultAsync();

                var product = await _context.Products.FindAsync(request.ProductId);

                // Nếu sp đã có cộng dồn số lượng
                if (detail != null)
                {
                    var quantity = detail.Quantity + request.Quantity;

                    // Kiểm tra số lượng tồn kho từ Redis
                    int currentStock = await _redisInventoryService.GetStockLevelAsync(request.ProductId);

                    if (currentStock == -1)
                    {
                        //lỗi từ redis
                        return Result<bool>.Failure("Lỗi khi truy cập thông tin tồn kho.", StatusCodes.Status500InternalServerError);
                    }


                    if (quantity > product.Quantity)
                    {
                        return Result<bool>.Failure("Số lượng sản phẩm tồn kho không đủ!", StatusCodes.Status400BadRequest);
                    }

                    detail.Quantity = detail.Quantity + request.Quantity;
                    detail.Profit = null;
                    _context.Set<DetailOrder>().Update(detail);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    int currentStock = await _redisInventoryService.GetStockLevelAsync(request.ProductId);

                    if (currentStock == -1)
                    {
                        //lỗi từ redis
                        return Result<bool>.Failure("Lỗi khi truy cập thông tin tồn kho.", StatusCodes.Status500InternalServerError);
                    }
                    if (request.Quantity > product.Quantity)
                    {
                        return Result<bool>.Failure("Số lượng sản phẩm tồn kho không đủ!", StatusCodes.Status400BadRequest);
                    }

                    int? quantity = request.Quantity;

                    // Áp dụng chương trình khuyến mãi cho sản phẩm đơn
                    (Promotion promo, decimal? priceDiscoutMax, int? group) =
                        await BaseOrderApplyPromotion.
                            ApplyPromotionForSingleProduct(_context, product);

                    var newDetail = new DetailOrder
                    {
                        OrderId = cart.Id,
                        ProductId = request.ProductId,
                        Quantity = quantity,
                        Cost = product.Price,
                        ReducedPrice = priceDiscoutMax,
                        Price = product.Price - priceDiscoutMax,
                        Profit = null,
                        IsSelected = true,
                        GroupPromotion = group,
                    };
                    await _context.DetailOrders.AddAsync(newDetail);
                    await _context.SaveChangesAsync();

                   await HandleAfterAddProductToCart(_currentUserService.CustomerId);
                }

                await HandleAfterUpdateProductToCart(cart.Id);

                // Giảm số lượng tồn kho trong Redis
                long newStock = await _redisInventoryService.DecrementStockLevelAsync(request.ProductId, (int)request.Quantity);
                if (newStock == -1)
                {
                    // Xử lý lỗi, ví dụ: hoàn tác các thay đổi trong giỏ hàng
                    // Có thể ném một exception hoặc trả về một Result.Failure
                    return Result<bool>.Failure("Lỗi khi cập nhật thông tin tồn kho.", StatusCodes.Status500InternalServerError);
                }

                return Result<bool>.Success(true, StatusCodes.Status200OK);
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure(ex.Message, StatusCodes.Status500InternalServerError);
            }
        }
        

        public async Task<Result<bool>> CancelOrder(CancelOrderRequest request)
        {
            try
            {
                var validator = new CancelOrderValidator(_context, _currentUserService.CustomerId);
                var validationResult = await validator.ValidateAsync(request);

                if (validationResult.IsValid == false)
                {
                    var errorMessages = validationResult.Errors.Select(x => x.ErrorMessage).ToList();
                    return Result<bool>.Failure(errorMessages, StatusCodes.Status400BadRequest);
                }

                // Kiểm tra nếu không phải người dùng thì không có quyền
                if (_currentUserService.Type != CLAIMS_VALUES.TYPE_USER)
                {
                    return Result<bool>.Failure("Vui lòng đăng ký tài khoản người dùng để đặt hàng!", StatusCodes.Status403Forbidden);
                }

                var order = await _context.Orders.FindAsync(request.OrderId);

                order.Status = Order.OrderStatus.Cancel;

                _context.Orders.Update(order);
                await _context.SaveChangesAsync();

                return Result<bool>.Success(true, StatusCodes.Status200OK);
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure(ex.Message, StatusCodes.Status500InternalServerError);
            }
        }


        private async Task HandleAfterChangeStatusOrderUpdateQuantityProduct(ChangeStatusOrderRequest? Request, OrderStatus? OldStatus)
        {
            if (OldStatus == OrderStatus.Order &&
               Request.Status == OrderStatus.Approve)
            {
                var order = await _context.Orders.FindAsync(Request.OrderId);

                var details = await _context.DetailOrders
                            .Where(x => x.OrderId == order.Id)
                            .ToListAsync();

                foreach (var item in details)
                {
                    // Cập nhật số lượng sản phẩm
                    var parent = await _context.Products.FindAsync(item.ProductId);
                    parent.Quantity -= item.Quantity;
                    _context.Products.Update(parent);
                    await _context.SaveChangesAsync();// nếu dùng reds thì bỏ 3 cái trên



                    //Cập nhật số lượng sản phẩm trong redis 
                    await _redisInventoryService.DecrementStockLevelAsync(item.ProductId, item.Quantity ?? 0);

                    var productsSingle = await _context.Products
                            .Where(x => x.ParentId == item.ProductId)
                            .ToListAsync();
                    // Tính toán lợi nhuận cho từng sản phẩm trong đơn hàng
                    decimal? profit = 0;
                    int? quantity = item.Quantity;
                    foreach (var product in productsSingle)
                    {
                        if (quantity <= product.Quantity)
                        {
                            profit += item.Price * quantity - product.Price * quantity;

                            // Cập nhật lại số lượng sản phẩm trên sp ảo
                            product.Quantity -= quantity;
                            _context.Products.Update(product);
                            await _context.SaveChangesAsync();

                            break;
                        }
                        profit += item.Price * product.Quantity - product.Price * product.Quantity;
                        quantity -= product.Quantity;

                        // Xoá sản phẩm ảo
                        product.Quantity = 0;
                        _context.Products.Update(product);
                        await _context.SaveChangesAsync();
                    }
                    item.Profit = profit;
                    _context.DetailOrders.Update(item);
                    await _context.SaveChangesAsync();
                }
            }

            await Task.CompletedTask;
        }

        public async Task<Result<bool>> ChangeStatusOrder(ChangeStatusOrderRequest request)
        {
            try
            {
                var validator = new ChangeStatusOrderValidator(_context);
                var validationResult = await validator.ValidateAsync(request);

                if (validationResult.IsValid == false)
                {
                    var errorMessages = validationResult.Errors.Select(x => x.ErrorMessage).ToList();
                    return Result<bool>.Failure(errorMessages, StatusCodes.Status400BadRequest);
                }

                var order = await _context.Orders.FindAsync(request.OrderId);

                OrderStatus? oldStatus = order.Status;

                order.Status = request.Status;
                if (_currentUserService.Type == CONSTANT_CLAIM_TYPES.Staff)
                {
                    order.StaffApprovedId = _currentUserService.StaffId;
                }

                _context.Orders.Update(order);
                await _context.SaveChangesAsync();

                // Sự kiện sau khi xác nhận đơn hàng
                await HandleAfterChangeStatusOrderUpdateQuantityProduct(request, oldStatus);

                return Result<bool>.Success(true, StatusCodes.Status200OK);
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure(ex.Message, StatusCodes.Status500InternalServerError);
            }
        }


        public async Task HandleAfterCreateOrderUpdateCartEvent(int? CartId, List<int?>? ProductsId)
        {
            var cart = await _context.Orders.FindAsync(CartId);

            int? couponId = cart.CouponId;

            cart.TotalAmount = 0;
            cart.TotalDecrease = 0;
            cart.Total = 0;
            cart.CouponId = null;

            _context.Orders.Update(cart);
            await _context.SaveChangesAsync();

            if (couponId != null)
            {
                var coupon = await _context.Coupons.FindAsync(couponId);
                coupon.Limit -= 1;
                _context.Coupons.Update(coupon);
                await _context.SaveChangesAsync();
            }

            await Task.CompletedTask;
        }
    
    public async Task<Result<bool>> CreateOrder(CreateOrderRequest request)
        {
            try
            {
                var validator = new CreateOrderValidator(_context, _currentUserService.CustomerId);
                var validationResult = await validator.ValidateAsync(request);

                if (validationResult.IsValid == false)
                {
                    var errorMessages = validationResult.Errors.Select(x => x.ErrorMessage).ToList();
                    return Result<bool>.Failure(errorMessages, StatusCodes.Status400BadRequest);
                }

                // Kiểm tra nếu không phải người dùng thì không có quyền
                if (_currentUserService.Type != CLAIMS_VALUES.TYPE_USER)
                {
                    return Result<bool>.Failure("Vui lòng đăng ký tài khoản người dùng để đặt hàng!", StatusCodes.Status403Forbidden);
                }

                // Lấy giỏ hàng của người dùng này
                var cart = await _context.Orders
                    .Where(x => x.CustomerId == _currentUserService.CustomerId &&
                                x.Status == OrderStatus.Cart)
                    .FirstOrDefaultAsync();

                if (cart == null)
                {
                    return Result<bool>.Failure("Vui lòng thêm sản phẩm vào giỏ hàng trước khi đặt hàng!", StatusCodes.Status400BadRequest);
                }

                // Tạo đơn hàng
                DateTime create = DateTime.Now;
                var order = new Order
                {
                    InternalCode = CommonService.InternalCodeGeneration("ORDER", create),
                    Date = create,
                    TotalAmount = cart.TotalAmount,
                    TotalDecrease = cart.TotalDecrease,
                    Total = cart.Total,
                    Message = request.Message,
                    Status = Order.OrderStatus.Order,
                    Type = Order.OrderType.Online,
                    IsPay = false,
                    CustomerId = _currentUserService.CustomerId,
                    CouponId = cart.CouponId,
                };
                var orderEntity = await _context.Orders.AddAsync(order);
                await _context.SaveChangesAsync();

                // Tạo chi tiết đơn hàng
                var details = await _context.DetailOrders
                        .Where(x => x.OrderId == cart.Id &&
                                    x.IsSelected == true)
                        .Select(x => new DetailOrder
                        {
                            Id = x.Id,
                            Cost = x.Cost,
                            ReducedPrice = x.ReducedPrice,
                            Price = x.Price,
                            Quantity = x.Quantity,
                            ProductId = x.ProductId,
                            OrderId = orderEntity.Entity.Id,
                            GroupPromotion = x.GroupPromotion,
                        }).ToListAsync();

                if (details == null)
                {
                    return Result<bool>.Failure("Vui lòng thêm sản phẩm vào giỏ hàng trước khi đặt hàng!", StatusCodes.Status400BadRequest);
                }

                _context.DetailOrders.UpdateRange(details);
                await _context.SaveChangesAsync();

                // Cập nhật lại thành tiền cho đơn hàng
                //order.TotalAmount = details.Sum(x => x.Cost * x.Quantity ?? 0);
                //order.TotalDecrease = details.Sum(x => x.ReducedPrice * x.Quantity ?? 0);
                //order.Total = details.Sum(x => x.Price * x.Quantity ?? 0);
                //_context.Orders.Update(order);
                //await _context.SaveChangesAsync(cancellationToken);

                await HandleAfterCreateOrderUpdateCartEvent(cart.Id, details.Select(x => x.ProductId).ToList());

                return Result<bool>.Success(true, StatusCodes.Status200OK);
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure(ex.Message, StatusCodes.Status500InternalServerError);
            }
        }

        public async Task<Result<CartDto>> DetailCart()
        {
            if (_currentUserService.Type != CLAIMS_VALUES.TYPE_USER)
            {
                return Result<CartDto>.Failure("Vui lòng đăng ký tài khoản người dùng!",
                    StatusCodes.Status403Forbidden);
            }

            var query = _context.Set<Order>()
                .FilterDeleted()
                .Where(x => x.CustomerId == _currentUserService.CustomerId &&
                            x.Status == Order.OrderStatus.Cart);


            query = query
            .Include(x => x.Payment)
            .Include(x => x.Customer)
            .Include(x => x.Delivery)
            .Include(x => x.StaffApproved);
            var findEntity = await query.SingleOrDefaultAsync();

            var dto = _mapper.Map<CartDto>(findEntity);

            if (dto != null)
            {
                var details = await _context.DetailOrders
                    .Include(x => x.Product)
                    .Where(x => x.OrderId == dto.Id).ToListAsync();
                dto.Details = _mapper.Map<List<DetailCartDto>>(details);
            }
            return Result<CartDto>.Success(dto, StatusCodes.Status200OK);
        }

        public async Task<Result<OrderDto>> DetailOrder(DetailBaseCommand request)
        {
            try
            {
                // Validate ID
                var validator = new DetailBaseValidator(_context);
                var validationResult = await validator.ValidateAsync(request);

                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                    return Result<OrderDto>.Failure(errors, StatusCodes.Status400BadRequest);
                }

        
                var order = _context.Set<Order>().FilterDeleted().Where(x => x.Id == request.Id);
                if (_currentUserService.Type == CLAIMS_VALUES.TYPE_USER)
                {
                    order = order.Where(x => x.CustomerId == _currentUserService.CustomerId);
                }

                order = order
                        .Include(x => x.Payment)
                        .Include(x => x.Customer)
                        .Include(x => x.Delivery)
                        .Include(x => x.StaffApproved);

                var findEntity = await order.SingleOrDefaultAsync();

                if (findEntity is null)
                {
                    return Result<OrderDto>.Failure(ValidatorTransform.NotExistsValue(Modules.Id, request.Id.ToString()),
                     StatusCodes.Status404NotFound);
                }



                // Map to DTO

                var orderDto = _mapper.Map<OrderDto>(findEntity);
                var detailsOrder = await _context.DetailOrders
                 .Include(x => x.Product)
                 .Where(x => x.OrderId == orderDto.Id).ToListAsync();
                orderDto.Details = _mapper.Map<List<DetailOrderDto>>(detailsOrder);

                return Result<OrderDto>.Success(orderDto, StatusCodes.Status200OK);
            }
            catch (Exception ex)
            {
                return Result<OrderDto>.Failure($"An error occurred: {ex.Message}", StatusCodes.Status500InternalServerError);
            }
        }

        public async Task<PaginatedResult<List<OrderDto>>> GetList(ListBaseCommand request)
        {
            try
            {
                var validator = new ListBaseCommandValidator(_context);
                var validationResult = await validator.ValidateAsync(request);

                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                    return PaginatedResult<List<OrderDto>>.Failure(StatusCodes.Status400BadRequest, errors);

                }
                var query = _context.Set<Order>().FilterDeleted();

                //var query = _context.Categories.AsQueryable();


                // Apply Sieve
                var sieveModel = new SieveModel
                {
                    Page = request.Page,
                    PageSize = request.PageSize,
                    Filters = request.Filters
                };

                sieveModel = _mapper.Map<SieveModel>(request);


                //var categories = await _sieveProcessor.Apply(sieveModel, query).ToListAsync();


                var totalCount = await PaginatedResultBase.CountApplySieveAsync(_sieveProcessor, sieveModel, query);
                //var totalCount = await query.CountAsync();


                var paginatedQuery = _sieveProcessor.Apply(sieveModel, query);

                var orders = await paginatedQuery.Skip((request.Page.Value - 1) * request.PageSize.Value)
                                                .Take(request.PageSize.Value)
                                                .ToListAsync();



                var orderDtos = _mapper.Map<List<OrderDto>>(orders);
                var paginatedResult = PaginatedResult<List<OrderDto>>.Create(orderDtos, totalCount, request.Page.Value, request.PageSize.Value, StatusCodes.Status200OK);

                return paginatedResult;
            }
            catch (Exception ex)
            {
                return PaginatedResult<List<OrderDto>>.Failure(StatusCodes.Status500InternalServerError, new List<string> { ex.Message });
            }
        }

        public async Task<Result<bool>> RemoveProductInCart(RemoveProductInCartRequest request)
        {
            // Kiểm tra nếu không phải người dùng thì không có quyền
            if (_currentUserService.Type != CLAIMS_VALUES.TYPE_USER)
            {
                return Result<bool>.Failure("Vui lòng đăng ký tài khoản người dùng để đặt hàng!", StatusCodes.Status403Forbidden);
            }

            try
            {
                // Lấy giỏ hàng của người dùng
                var cart = await _context.Orders
                    .Include(x => x.Customer).ThenInclude(x => x.User)
                    .Where(x => x.Customer.User.Id == _currentUserService.UserId &&
                                x.Status == Order.OrderStatus.Cart)
                    .FirstOrDefaultAsync();

                var validator = new RemoveProductInCartValidator(_context, cart.Id);
                var validationResult = await validator.ValidateAsync(request);

                if (validationResult.IsValid == false)
                {
                    var errorMessages = validationResult.Errors.Select(x => x.ErrorMessage).ToList();
                    return Result<bool>.Failure(errorMessages, StatusCodes.Status400BadRequest);
                }

                var detailOrder = await _context.DetailOrders
                            .Where(x => x.ProductId == request.ProductId &&
                                        x.OrderId == cart.Id)
                            .FirstOrDefaultAsync();
                _context.DetailOrders.Remove(detailOrder);
                await _context.SaveChangesAsync();

                await HandleAfterUpdateProductToCart(cart.Id);

                return Result<bool>.Success(true, StatusCodes.Status200OK);
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure(ex.Message, StatusCodes.Status500InternalServerError);
            }
        }

        public async Task<Result<bool>> UpdateProductInCart(UpdateProductInCartRequest request)
        {
            try
            {
                var validator = new UpdateProductInCartValidator(_context);
                var validationResult = await validator.ValidateAsync(request);

                if (validationResult.IsValid == false)
                {
                    var errorMessages = validationResult.Errors.Select(x => x.ErrorMessage).ToList();
                    return Result<bool>.Failure(errorMessages, StatusCodes.Status400BadRequest);
                }

                // Kiểm tra nếu không phải người dùng thì không có quyền
                if (_currentUserService.Type != CLAIMS_VALUES.TYPE_USER)
                {
                    return Result<bool>.Failure("Vui lòng đăng ký tài khoản người dùng để đặt hàng!", StatusCodes.Status403Forbidden);
                }

                // Lấy giỏ hàng của người dùng
                var cart = await _context.Orders
                    .Include(x => x.Customer).ThenInclude(x => x.User)
                    .Where(x => x.Customer.User.Id == _currentUserService.UserId &&
                                x.Status == OrderStatus.Cart)
                    .FirstOrDefaultAsync();

                var detail = await _context.DetailOrders
                            .Where(x => x.ProductId == request.ProductId &&
                                        x.OrderId == cart.Id)
                            .FirstOrDefaultAsync();

                // Cập nhật lại số lượng sản phẩm và lợi nhuận
                if (detail != null)
                {
                    var product = await _context.Products.FindAsync(request.ProductId);
                    int? quantity = request.Quantity;

                    detail.Quantity = request.Quantity;
                    detail.IsSelected = request.IsSelected == true ? true : false;
                    _context.DetailOrders.Update(detail);
                    await _context.SaveChangesAsync();
                }

                await HandleAfterUpdateProductToCart(cart.Id);

                return Result<bool>.Success(true, StatusCodes.Status200OK);
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure(ex.Message, StatusCodes.Status500InternalServerError);
            }
        }

 
    }
}
