using api_be.Application.Models.Request;
using api_be.Domain.Transforms;
using System.Reflection;
using api_be.Infrastructure.DB;
using FluentValidation;

namespace api_be.Application.Models.ValidatorRequest
{
    public class CreatePermissionValidator : AbstractValidator<CreatePermissionRequest>
    {
        public CreatePermissionValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage(ValidatorTransform.Required(Modules.Permission.Module))
                .MaximumLength(Modules.NamePermissionMin).WithMessage(ValidatorTransform.MinimumLength(Modules.Permission.Module, Modules.NamePermissionMin));
        }
    }

}
