using api_be.Core.Domain.Interfaces;
using api_be.Domain.Transforms;
using static api_be.Core.Entities.Order;
using api_be.Infrastructure.DB;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace api_be.Application.Models.ValidatorRequest.DeliveryValidator.BaseDelivery
{
    public class BaseDeliveryValidator : AbstractValidator<IBaseDelivery>
    {
        public BaseDeliveryValidator(ISupermarketDbContext pContext)
        {
            RuleFor(x => x.OrderId)
                .MustAsync(async (orderId, token) =>
                {
                    return await pContext.Orders
                            .AnyAsync(x => x.Id == orderId &&
                                        x.Status == OrderStatus.Approve);
                }).WithMessage("Id đơn hàng không hợp lệ!");

            RuleFor(x => x.From)
                .NotEmpty().WithMessage(ValidatorTransform.Required("From"))
                .MaximumLength(Modules.AddressMax).WithMessage("Địa chỉ giao hàng tối đa 500 kí tự!");

            RuleFor(x => x.To)
                .NotEmpty().WithMessage(ValidatorTransform.Required("To"))
                .MaximumLength(Modules.AddressMax).WithMessage("Địa chỉ nhận hàng tối đa 500 kí tự!");

            RuleFor(x => x.TransportFee)
                .Must(x => 0 <= x).WithMessage("Phí vận chuyển không được âm!");
        }
    }
}
