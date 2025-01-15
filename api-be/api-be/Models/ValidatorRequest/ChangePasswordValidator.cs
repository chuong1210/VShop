using api_be.Domain.Interfaces;
using api_be.Entities.Auth;
using api_be.Exceptions;
using api_be.Models.Request;
using api_be.Transforms;
using Azure.Core;
using FluentValidation;
using Microsoft.AspNetCore.Identity;

namespace api_be.Models.ValidatorRequest
{
     




    public class ChangePasswordValidator : AbstractValidator<ChangePasswordRequest>
    {
        public ChangePasswordValidator(ISupermarketDbContext context, IPasswordHasher<User> _passwordHasher)
        {

            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage(ValidatorTransform.Required(Modules.User.Id));


            RuleFor(x => x.CurrentPassword)
              .NotEmpty().WithMessage(ValidatorTransform.Required(Modules.User.CurrentPassword))
              .MustAsync(async (_ChangePasswordRequest, id, cancellationToken, predict) => {
                  var user = await context.Users.FindAsync(_ChangePasswordRequest.UserId);

                  if (user == null ) // return false;
                      throw new BadRequestException(ValidatorTransform.NotExistsValue(Modules.User.Module,
                            _ChangePasswordRequest.UserId.ToString())); ;

                  var result = _passwordHasher.VerifyHashedPassword(user, user.Password, _ChangePasswordRequest.CurrentPassword);
                  return result == PasswordVerificationResult.Success;
              })
              .WithMessage(ValidatorTransform.Equal(Modules.User.CurrentPassword, Modules.User.ConfirmPassword));

            RuleFor(p => p.NewPassword)
                    .NotEmpty().WithMessage(ValidatorTransform.Required(Modules.User.Password))
                    .MinimumLength(Modules.User.PasswordMin)
                    .WithMessage(ValidatorTransform.MinimumLength(Modules.User.Password, Modules.User.PasswordMin))
                    .MaximumLength(Modules.User.UserNameMax)
                    .WithMessage(ValidatorTransform.MaximumLength(Modules.User.Password, Modules.User.UserNameMax));

            RuleFor(x => x.ConfirmPassword)
                .Equal(x => x.NewPassword).WithMessage(ValidatorTransform.Equal(Modules.User.Password, Modules.User.ConfirmPassword));
        }
    }

}
