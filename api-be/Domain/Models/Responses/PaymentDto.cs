using api_be.Domain.Models.Common;

namespace api_be.Domain.Models.Responses
{
    public record PaymentDto : BaseDto
    {
        public string? InternalCode { get; set; }

        public string? Name { get; set; }
    }
}
