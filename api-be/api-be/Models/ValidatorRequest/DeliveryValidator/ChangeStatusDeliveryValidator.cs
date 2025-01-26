using api_be.Domain.Interfaces;
using api_be.Models.Request.DeliveryRequest;
using static api_be.Entities.Delivery;

namespace api_be.Models.ValidatorRequest.DeliveryValidator
{
    public class ChangeStatusDeliveryValidator : AbstractValidator<ChangeStatusDeliveryRequest>
    {
        public ChangeStatusDeliveryValidator(ISupermarketDbContext pContext)
        {
            RuleFor(x => x.DeliveryId)
                .MustAsync(async (DeliveryId, token) =>
                {
                    return await pContext.Deliveries
                            .AnyAsync(x => x.Id == DeliveryId);
                }).WithMessage("Id đơn hàng không hợp lệ!");

            RuleFor(x => x.Status)
                .MustAsync(async (request, status, token) =>
                {
                    var prepare = await pContext.Deliveries.FindAsync(request.DeliveryId);

                    if (prepare == null)
                    {
                        return false;
                    }

                    if (!IsValidTransition(prepare.Status, status))
                    {
                        return false;
                    }

                    return true;
                }).WithMessage("Trạng thái thay đổi đơn hàng không hợp lệ!");
        }
        private static bool IsValidTransition(DeliveryStatus? currentStatus, DeliveryStatus? newStatus)
        {
            return (currentStatus == DeliveryStatus.Prepare && newStatus == DeliveryStatus.Transport) ||
                   (currentStatus == DeliveryStatus.Transport && newStatus == DeliveryStatus.Delivered) ||
                   (currentStatus == DeliveryStatus.Delivered &&
                    (newStatus == DeliveryStatus.Received || newStatus == DeliveryStatus.Cancel));
        }
    }


}

