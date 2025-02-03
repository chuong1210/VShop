using api_be.Core.Domain.Interfaces;
using api_be.Domain.Models.Request;
using api_be.Domain.Transforms;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using api_be.Infrastructure.DB;

namespace api_be.Application.ValidatorRequest
{
    public class ForgotPasswordValidator : AbstractValidator<ForgotPasswordRequest>
    {
        private readonly ISupermarketDbContext _context;

        public ForgotPasswordValidator(ISupermarketDbContext context)
        {
            _context = context;

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage(ValidatorTransform.Required(Modules.Email))
                .EmailAddress().WithMessage(x => ValidatorTransform.ValidValue(Modules.Email, x.Email)) // Truyền giá trị email động

                .Matches(@"^[^\s@]+@[^\s@]+\.[^\s@]+$")
                .WithMessage(x => ValidatorTransform.ValidValue(Modules.Email, x.Email))
                .MustAsync(async (email, cancellation) =>
                {
                    var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
                    return user != null;
                }).WithMessage(x=> ValidatorTransform.NotExistsValue(Modules.Email,x.Email));
        }
    }

}
