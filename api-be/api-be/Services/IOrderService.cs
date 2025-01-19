using api_be.Entities;
using api_be.Models.Request;
using api_be.Models.Request.OrderRequest;
using api_be.Models.Responses;
using api_be.Models.ValidatorRequest.DefaultBase;
using api_be.ValidatorRequest.DefaultBase;

namespace api_be.Services
{
    public interface IOrderService
    {
        public Task<Result<bool>> CreateOrder(CreateOrderRequest request);
        public Task<Result<bool>> AddCouponToCart(AddCouponToCartRequest request);
        public Task<Result<bool>> AddProductToCart(AddProductToCartRequest request);
        public Task<Result<bool>> CancelOrder(CancelOrderRequest request);
        public Task<Result<bool>> ChangeStatusOrder(ChangeStatusOrderRequest request);
        public Task<Result<Boolean>> UpdateInCart(UpdateProductInCartRequest request);
        public Task<Result<Boolean>> RemoveProductInCart(RemoveProductInCartRequest id);
        public Task<Result<OrderDto>> DetailOrder(DetailBaseCommand request);
        public Task<Result<CartDto>> DetailCart(DetailBaseCommand request);

        public Task<PaginatedResult<List<OrderDto>>> GetList(ListBaseCommand request);
    }
}
