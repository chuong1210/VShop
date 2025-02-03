using api_be.Core.Domain.Interfaces;
using api_be.Domain.Models.Request;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using api_be.Infrastructure.DB;

namespace api_be.Application.ValidatorRequest
{
    public class ResendVerificationEmailValidator : AbstractValidator<ResendVerificationEmailRequest>
    {
        private readonly ISupermarketDbContext _context;

        public ResendVerificationEmailValidator(ISupermarketDbContext context)
        {
            _context = context;

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email không được để trống")
                .EmailAddress().WithMessage("Email không đúng định dạng")
                .MustAsync(async (email, cancellation) =>
                {
                    var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
                    return user != null;
                }).WithMessage("Email không tồn tại trong hệ thống");
        }
    }
}
