using api_be.Core.Domain.Interfaces;
using api_be.Core.Entities;
using api_be.Domain.Models.Request.OrderRequest;
using api_be.Infrastructure.DB;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace api_be.Application.ValidatorRequest.OrderValidator
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
