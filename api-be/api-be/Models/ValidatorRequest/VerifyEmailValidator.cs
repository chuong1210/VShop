using api_be.Models.Request;
using FluentValidation;

namespace api_be.Models.ValidatorRequest
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
