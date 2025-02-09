using api_be.Application.Models.Common;

namespace api_be.Application.Responses.PaymentResponse
{
    public record PaymentDto : BaseDto
    {
        public string? InternalCode { get; set; }

        public string? Name { get; set; }
    }
}
