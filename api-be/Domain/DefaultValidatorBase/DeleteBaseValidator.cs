using api_be.Core.Domain.Interfaces;
using api_be.Domain.Models.Common;
using api_be.Domain.Transforms;
using FluentValidation;
using api_be.Infrastructure.DB;

namespace api_be.Domain.DefaultValidatorBase
{
    public class DeleteBaseValidator<T> : AbstractValidator<T> where T : BaseDto
    {
        public DeleteBaseValidator(ISupermarketDbContext? pContext)
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage(ValidatorTransform.Required(Modules.Id));
        }
    }
}