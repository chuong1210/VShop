using static api_be.Core.Entities.Delivery;

namespace api_be.Domain.Models.Request.DeliveryRequest
{
    public record ChangeStatusDeliveryRequest
    {
        public int? DeliveryId { get; set; }

        public DeliveryStatus? Status { get; set; }
    }
}
