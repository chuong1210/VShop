using api_be.Core.Domain.Interfaces;
using api_be.Application.Models.Request.OrderRequest;
using api_be.Domain.Transforms;
using static api_be.Core.Entities.Product;
using api_be.Infrastructure.DB;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace api_be.Application.Models.ValidatorRequest.OrderValidator
{
    public class AddProductToCartValidator : AbstractValidator<AddProductToCartRequest>
    {
        public AddProductToCartValidator(ISupermarketDbContext pContext)
        {
            RuleFor(x => x.ProductId)
                .MustAsync(async (productId, token) =>
                {
                    return await pContext.Products
                            .AnyAsync(x => x.Id == productId &&
                                           x.Type == ProductType.Option &&
                                           x.Status == ProductStatus.Active &&
                                            x.IsDeleted == false);
                }).WithMessage(ValidatorTransform.NotExists(Modules.Order.ProductId));

            RuleFor(x => x.Quantity)
                .GreaterThanOrEqualTo(Modules.Order.MinQuantity)
                .WithMessage(ValidatorTransform.GreaterThanOrEqualTo(Modules.Order.Quantity, Modules.Order.MinQuantity));
        }
    }
}
