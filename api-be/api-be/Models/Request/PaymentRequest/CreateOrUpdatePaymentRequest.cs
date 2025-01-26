using api_be.Domain.Interfaces;
using api_be.Models.ValidatorRequest.DefaultBase;

namespace api_be.Models.Request.PaymentRequest
{
    public record CreateOrUpdatePaymentRequest:UpdateBaseCommand,IBasePayment
    {

        public string? InternalCode { get; set; }

        public string? Name { get; set; }
    }
}
