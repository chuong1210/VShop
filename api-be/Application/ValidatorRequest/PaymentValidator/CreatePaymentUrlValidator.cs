using api_be.Core.Domain.Interfaces;
using api_be.Domain.Models.Request.PaymentRequest;
using api_be.Infrastructure.DB;
using FluentValidation;

namespace api_be.Application.ValidatorRequest.PaymentValidator
{
    public class CreatePaymentUrlValidator : AbstractValidator<CreatePaymentUrlRequest>
    {
        public CreatePaymentUrlValidator()
        {
            RuleFor(x => x.Amount)
           .GreaterThan(0).WithMessage("Số tiền phải lớn hơn 0.")
           .LessThanOrEqualTo(1_000_000_000).WithMessage("Số tiền không được vượt quá 1 tỷ.");

            RuleFor(x => x.Description)
                .NotEmpty().WithMessage("Mô tả không được để trống.")
                .MaximumLength(255).WithMessage("Mô tả không được dài hơn 255 ký tự.");
        }
    }
}
