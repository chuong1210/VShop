using api_be.Domain.Interfaces;
using api_be.Transforms;
using FluentValidation;

namespace api_be.ValidatorRequest.ListBase
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