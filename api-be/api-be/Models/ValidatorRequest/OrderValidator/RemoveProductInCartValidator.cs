using api_be.Domain.Interfaces;
using api_be.Models.Request.OrderRequest;
using api_be.Transforms;

namespace api_be.Models.ValidatorRequest.OrderValidator
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
