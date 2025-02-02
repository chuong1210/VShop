using api_be.Core.Domain.Interfaces;
using api_be.Core.Entities.Auth;
using api_be.Domain.Models.Request;
using api_be.Domain.Transforms;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using api_be.Infrastructure.DB;
using Microsoft.Extensions.Configuration;

namespace api_be.Application.ValidatorRequest
{
    public class ResetPasswordValidator : AbstractValidator<ResetPasswordRequest>
    {
        private readonly ISupermarketDbContext _context;
        private readonly IConfiguration _configuration;

        public ResetPasswordValidator(ISupermarketDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;

            RuleFor(x => x.Token)
                .NotEmpty().WithMessage(ValidatorTransform.Required("Token"));

            RuleFor(x => x.NewPassword)
            .NotEmpty()
            .WithMessage(ValidatorTransform.Required(Modules.User.NewPassword))
            .MinimumLength(8)
            .WithMessage(ValidatorTransform.MinimumLength(Modules.User.NewPassword, 8))
            .Matches("[A-Z]")
            .WithMessage(ValidatorTransform.AnyIsUpper(Modules.User.NewPassword))
            .Matches("[a-z]")
            .WithMessage(ValidatorTransform.AnyIsLower(Modules.User.NewPassword))
            .Matches("[0-9]")
            .WithMessage(ValidatorTransform.AnyIsDigit(Modules.User.NewPassword))
            .Matches("[^a-zA-Z0-9]")
            .WithMessage(ValidatorTransform.AnyIsLetterOrDigit(Modules.User.NewPassword));
            RuleFor(x => x.ConfirmPassword)
                      .NotEmpty()
                      .WithMessage(ValidatorTransform.Required(Modules.User.ConfirmPassword))
                      .Equal(x => x.NewPassword)
                      .WithMessage(ValidatorTransform.Equal(Modules.User.ConfirmPassword, Modules.User.NewPassword));

        RuleFor(x => x.Token)
                .MustAsync(async (token, cancellation) =>
                {
                    var resetToken = await _context.Set<UserVerification>()
                        .FirstOrDefaultAsync(t => t.Token == token);
                    return resetToken != null && !resetToken.IsUsed && resetToken.ExpiryDate > DateTime.UtcNow;
                }).WithMessage(IdentityTransform.InvalidAccessToken());
        }
    }
}
