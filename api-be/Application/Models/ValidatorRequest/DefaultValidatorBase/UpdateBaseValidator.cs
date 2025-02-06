

using api_be.Core.Domain.Interfaces;
using api_be.Domain.Transforms;
using FluentValidation;
using static Sieve.Extensions.MethodInfoExtended;
using api_be.Infrastructure.DB;
using api_be.Application.Models.Common;
namespace api_be.Application.Models.ValidatorRequest.DefaultValidatorBase
{
    public record UpdateBaseCommand : BaseDto
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
