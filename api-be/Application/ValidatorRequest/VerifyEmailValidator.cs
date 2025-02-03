using api_be.Domain.Models.Request;
using FluentValidation;
using api_be.Infrastructure.DB;

namespace api_be.Application.ValidatorRequest
{
    public class VerifyEmailValidator : AbstractValidator<VerifyEmailRequest>
    {
        public VerifyEmailValidator()
        {
            RuleFor(x => x.Token)
                .NotEmpty().WithMessage("Token xác nhận không được để trống");
        }
    }
}
