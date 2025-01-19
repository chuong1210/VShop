using api_be.Domain.Interfaces;
using api_be.Entities;
using api_be.Models.Request.OrderRequest;

namespace api_be.Models.ValidatorRequest.OrderValidator
{
    public class CancelOrderValidator : AbstractValidator<CancelOrderRequest>
    {
        public CancelOrderValidator(ISupermarketDbContext pContext, int? pCustomerId)
        {
            RuleFor(x => x.OrderId)
                .MustAsync(async (orderId, token) =>
                {
                    return await pContext.Orders
                            .AnyAsync(x => x.Id == orderId &&
                                        x.CustomerId == pCustomerId &&
                                        x.Status == Order.OrderStatus.Order);
                }).WithMessage("Id đơn hàng không hợp lệ!");
        }
    }
}
