using api_be.Domain.Models.Common;
using static api_be.Core.Entities.Delivery;

namespace api_be.Domain.Models.Responses
{
    public record DeliveryDto : BaseDto
    {
        public string? InternalCode { get; set; }

        public DateTime? DateSent { get; set; }

        public string? From { get; set; }

        public DateTime? DateReceipt { get; set; }

        public string? To { get; set; }

        public decimal? TransportFee { get; set; }

        public DeliveryStatus? Stutus { get; set; }

        public int? PackingStaffId { get; set; }

        public StaffDto? PackingStaff { get; set; }

        public int? ShipperId { get; set; }

        public StaffDto? Shipper { get; set; }
    }
}
