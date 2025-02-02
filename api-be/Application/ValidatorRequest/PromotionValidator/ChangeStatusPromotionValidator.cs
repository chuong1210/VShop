using api_be.Domain.Models.Request.PromotionRequest;
using api_be.Domain.Transforms;
using api_be.Infrastructure.DB;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static api_be.Core.Entities.Promotion;

namespace api_be.Application.ValidatorRequest.PromotionValidator
{
    public class ChangeStatusPromotionValidator : AbstractValidator<ChangeStatusPromotionRequest>
    {
        public ChangeStatusPromotionValidator(ISupermarketDbContext pContext)
        {
            RuleFor(x => x.PromotionId)
                   .MustAsync(async (promotionId, token) =>
                   {
                       return promotionId == null ||
                       await pContext.Promotions.AnyAsync(x => x.Id == promotionId && x.IsDeleted == false);
                   }).WithMessage(ValidatorTransform.NotExists(Modules.Promotion.Id));

            var enumValues = Enum.GetValues(typeof(PromotionStatus))
                    .Cast<PromotionStatus>()
                    .Select(v => v.ToString())
                    .ToArray();

            RuleFor(x => x.Status)
                .IsInEnum()
                .WithMessage(ValidatorTransform.Must(Modules.Promotion.Status, string.Join(", ", enumValues)));
        }
    }
}
