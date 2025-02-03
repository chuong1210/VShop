using api_be.Core.Domain.Interfaces;
using api_be.Domain.Models.Request.PromotionRequest;
using api_be.Domain.Transforms;
using static api_be.Core.Entities.Promotion;
using api_be.Infrastructure.DB;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace api_be.Application.ValidatorRequest.PromotionValidator
{
    public class ApplyPromotionForProductValidator : AbstractValidator<ApplyPromotionForProductRequest>
    {
        public ApplyPromotionForProductValidator(ISupermarketDbContext pContext)
        {
            RuleFor(x => x.PromotionId)
                   .MustAsync(async (promotionId, token) =>
                   {
                       return promotionId == null ||
                       await pContext.Promotions.AnyAsync(x => x.Id == promotionId &&
                            x.Status == PromotionStatus.Draft);
                   }).WithMessage(ValidatorTransform.NotExists(Modules.Promotion.Id));

            RuleFor(x => x.Group)
                .MustAsync(async (x, group, token) =>
                {
                    // Danh sách sản phẩm null
                    if (x.ProductsId == null)
                    {
                        return false;
                    }

                    if (group != -1)
                    {
                        var exists = await pContext.PromotionProductRequirements
                            .AnyAsync(x => x.Group == group);
                        if (!exists)
                        {
                            return false;
                        }
                    }
                    foreach (var productId in x.ProductsId)
                    {
                        var product = await pContext.Products.FindAsync(productId);
                        if (product == null)
                        {
                            return false;
                        }
                    }
                    return true;
                }).WithMessage("Danh sách sản phẩm trong chương trình khuyến mãi không hợp lệ hoặc đã tồn tại!");
        }
    }
}
