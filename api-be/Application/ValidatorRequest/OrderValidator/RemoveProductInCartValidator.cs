using api_be.Core.Domain.Interfaces;
using api_be.Domain.Models.Request.OrderRequest;
using api_be.Domain.Transforms;
using api_be.Infrastructure.DB;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace api_be.Application.ValidatorRequest.OrderValidator
{
    public class RemoveProductInCartValidator : AbstractValidator<RemoveProductInCartRequest>
    {
     
            public RemoveProductInCartValidator(ISupermarketDbContext pContext, int? pCartId)
            {
                RuleFor(x => x.ProductId)
                    .MustAsync(async (productId, token) =>
                    {
                        return await pContext.DetailOrders
                                .AnyAsync(x => x.OrderId == pCartId &&
                                               x.ProductId == productId);
                    }).WithMessage(ValidatorTransform.NotExists(Modules.Order.ProductId));
            }
    }
}
