using api_be.Domain.Interfaces;
using static api_be.Entities.Order;
namespace api_be.Models.ValidatorRequest.OrderValidator.BaseOrders
{

    public class BaseOrderValidator<T> : AbstractValidator<T> where T:IBaseOrder
    {
        public BaseOrderValidator(ISupermarketDbContext pContext)
        {
            RuleFor(x => x.OrderId)
          .MustAsync(async (id, token) =>
          {
              return await pContext.Orders
              .AnyAsync(x => x.Id == id && x.Status != OrderStatus.Cart);
          }).WithMessage("Id của đơn hàng không hợp lệ!");
        }
    }
}
