using api_be.Models.Common;

namespace api_be.Models
{
    public record PaymentDto : BaseDto
    {
        public string? InternalCode { get; set; }

        public string? Name { get; set; }
    }
}
