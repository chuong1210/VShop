using api_be.Models.Request;
using api_be.Transforms;
using System.Reflection;

namespace api_be.Models.ValidatorRequest
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
