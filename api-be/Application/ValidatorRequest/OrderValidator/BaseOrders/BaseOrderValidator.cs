using api_be.Core.Domain.Interfaces;
using static api_be.Core.Entities.Order;
using api_be.Infrastructure.DB;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace api_be.Application.ValidatorRequest.OrderValidator.BaseOrders
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
