using static api_be.Entities.Delivery;

namespace api_be.Models.Request.DeliveryRequest
{
    public record ChangeStatusDeliveryRequest
    {
        public int? DeliveryId { get; set; }

        public DeliveryStatus? Status { get; set; }
    }
}
