using api_be.Domain.Interfaces;
using api_be.Models.Common;
using api_be.Transforms;
using FluentValidation;

namespace  api_be.Models.ValidatorRequest.DefaultBase
{
    public class DeleteBaseValidator<T> : AbstractValidator<T> where T : BaseDto
    {
        public DeleteBaseValidator(ISupermarketDbContext pContext)
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage(ValidatorTransform.Required(Modules.Id));
        }
    }
}