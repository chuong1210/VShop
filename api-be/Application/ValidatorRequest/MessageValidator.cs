using FluentValidation;
using api_be.Core.Entities;
using api_be.Domain.Models.Request;

namespace api_be.Application.ValidatorRequest
{
    public class MessageValidator : AbstractValidator<MessageRequest>
    {
        public MessageValidator()
        {
            // Kiểm tra SenderId không rỗng
            RuleFor(x => x.SenderId)
                .GreaterThan(0).WithMessage("SenderId must be a positive number");

            // Kiểm tra ReceiverId không rỗng
            RuleFor(x => x.ReceiverId)
                .GreaterThan(0).WithMessage("ReceiverId must be a positive number");

            // Kiểm tra Content không rỗng và có độ dài tối thiểu
            RuleFor(x => x.Content)
                .NotEmpty().WithMessage("Content is required")
                .MinimumLength(1).WithMessage("Content must have at least 1 character")
                .MaximumLength(500).WithMessage("Content cannot be more than 500 characters");

            // Kiểm tra SentAt không phải là ngày trong tương lai
            RuleFor(x => x.SentAt)
                .NotEmpty().WithMessage("SentAt is required")
                .LessThanOrEqualTo(DateTime.UtcNow).WithMessage("SentAt cannot be in the future");

            // Kiểm tra IsRead là kiểu boolean hợp lệ
            RuleFor(x => x.IsRead)
                .Must(value => value == true || value == false)
                .WithMessage("IsRead must be a valid boolean value (true or false)");
        }
    }
}
