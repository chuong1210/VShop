
using api_be.Models;
using api_be.Models.Request;
using api_be.Transforms;
using FluentValidation;

namespace api_be.Validator;

public class LoginAccountValidator : AbstractValidator<LoginAccountRequest>
{
    public LoginAccountValidator()
    {
        RuleFor(p => p.UserName)
            .NotEmpty().WithMessage(ValidatorTransform.Required(Modules.User.UserName))
            .MinimumLength(Modules.User.UserNameMin)
            .WithMessage(ValidatorTransform.MinimumLength(Modules.User.UserName, Modules.User.UserNameMin))
            .MaximumLength(Modules.User.UserNameMax)
            .WithMessage(ValidatorTransform.MaximumLength(Modules.User.UserName, Modules.User.UserNameMax));

        RuleFor(p => p.Password)
            .NotEmpty().WithMessage(ValidatorTransform.Required(Modules.User.Password))
            .MinimumLength(Modules.User.PasswordMin)
            .WithMessage(ValidatorTransform.MinimumLength(Modules.User.Password, Modules.User.PasswordMin))
            .MaximumLength(Modules.User.UserNameMax)
            .WithMessage(ValidatorTransform.MaximumLength(Modules.User.Password, Modules.User.UserNameMax));
    }
}
