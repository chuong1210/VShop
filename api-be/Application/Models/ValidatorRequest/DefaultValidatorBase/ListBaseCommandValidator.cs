using api_be.Core.Domain.Interfaces;
using api_be.Domain.Transforms;
using FluentValidation;
using api_be.Infrastructure.DB;

namespace api_be.Application.Models.ValidatorRequest.DefaultValidatorBase
{
    public class ListBaseCommandValidator : AbstractValidator<ListBaseCommand>
    {
        public ListBaseCommandValidator(ISupermarketDbContext pContext)
        {
            RuleFor(x => x.Page)
                 .GreaterThanOrEqualTo(Modules.PageNumberMin)
                 .WithMessage(ValidatorTransform.GreaterThanOrEqualTo(Modules.PageNumber, Modules.PageNumberMin));

            RuleFor(x => x.PageSize)
                .GreaterThanOrEqualTo(Modules.PageSizeMin)
                .WithMessage(ValidatorTransform.GreaterThanOrEqualTo(Modules.PageSize, Modules.PageSizeMin));
        }
    }
}