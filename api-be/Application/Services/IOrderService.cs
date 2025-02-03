using api_be.Core.Entities;
using api_be.Domain.Models.Request;
using api_be.Domain.Models.Request.OrderRequest;
using api_be.Domain.Models.Responses;
using api_be.Domain.DefaultValidatorBase;
using api_be.Domain.DefaultValidatorBase;

namespace api_be.Application.Services
{
    public interface IOrderService
    {
        public Task<Result<bool>> CreateOrder(CreateOrderRequest request);
        public Task<Result<bool>> AddCouponToCart(AddCouponToCartRequest request);
        public Task<Result<bool>> AddProductToCart(AddProductToCartRequest request);
        public Task<Result<bool>> CancelOrder(CancelOrderRequest request);
        public Task<Result<bool>> ChangeStatusOrder(ChangeStatusOrderRequest request);
        public Task<Result<Boolean>> UpdateProductInCart(UpdateProductInCartRequest request);
        public Task<Result<Boolean>> RemoveProductInCart(RemoveProductInCartRequest id);
        public Task<Result<OrderDto>> DetailOrder(DetailBaseCommand request);
        public Task<Result<CartDto>> DetailCart();

        public Task<PaginatedResult<List<OrderDto>>> GetList(ListBaseCommand request);
    }
}
