using api_be.Domain.Interfaces;
using api_be.Models.Request.OrderRequest;
using api_be.Transforms;
using static api_be.Entities.Product;

namespace api_be.Models.ValidatorRequest.OrderValidator
{
    public class UpdateProductInCartValidator : AbstractValidator<UpdateProductInCartRequest>
    {
        public UpdateProductInCartValidator(ISupermarketDbContext pContext)
        {
            RuleFor(x => x.ProductId)
                .MustAsync(async (productId, token) =>
                {
                    return await pContext.Products.AnyAsync(x => x.Id == productId && x.Type == ProductType.Option);
                }).WithMessage(ValidatorTransform.NotExists(Modules.Order.ProductId));

            RuleFor(x => x.Quantity)
                .GreaterThanOrEqualTo(Modules.Order.MinQuantity)
                .WithMessage(ValidatorTransform.GreaterThanOrEqualTo(Modules.Order.Quantity, Modules.Order.MinQuantity));
        }
    }
}
