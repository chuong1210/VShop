using api_be.Core.Domain.Interfaces;
using api_be.Domain.Models.Request;
using api_be.Domain.Transforms;
using  api_be.Application.ValidatorRequest.BaseUser;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using api_be.Infrastructure.DB;

namespace api_be.Application.ValidatorRequest
{
    public class CreateUserValidator : AbstractValidator<CreateUserRequest>
    {
        public CreateUserValidator(ISupermarketDbContext pContext)
        {
            Include(new BaseUserValidator(pContext));

            RuleFor(p => p.UserName)
                .NotEmpty().WithMessage(ValidatorTransform.Required(Modules.User.UserName))
                .MinimumLength(Modules.User.UserNameMin)
                .WithMessage(ValidatorTransform.MinimumLength(Modules.User.UserName, Modules.User.UserNameMin))
                .MaximumLength(Modules.User.UserNameMax)
                .WithMessage(ValidatorTransform.MaximumLength(Modules.User.UserName, Modules.User.UserNameMax))
                .MustAsync(async (userName, token) =>
                {
                    return await pContext.Users.AnyAsync(x => x.UserName == userName) == false;
                }).WithMessage(ValidatorTransform.Exists(Modules.User.UserName));

            RuleFor(p => p.Password)
                .NotEmpty().WithMessage(ValidatorTransform.Required(Modules.User.Password))
                .MinimumLength(Modules.User.PasswordMin)
                .WithMessage(ValidatorTransform.MinimumLength(Modules.User.Password, Modules.User.PasswordMin))
                .MaximumLength(Modules.User.UserNameMax)
                .WithMessage(ValidatorTransform.MaximumLength(Modules.User.Password, Modules.User.UserNameMax));
        }
    }

}