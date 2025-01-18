

using api_be.Domain.Interfaces;
using api_be.Models.Common;
using api_be.Transforms;
using FluentValidation;
using static Sieve.Extensions.MethodInfoExtended;

namespace api_be.Models.ValidatorRequest.DefaultBase
{
    public record UpdateBaseCommand:BaseDto
    {
    }
    public class UpdateBaseValidator<T> : AbstractValidator<T> where T : BaseDto
    {

        public UpdateBaseValidator(ISupermarketDbContext pContext)
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage(ValidatorTransform.Required(Modules.Id));
        }
    }
}
