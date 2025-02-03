using api_be.Core.Domain.Interfaces;
using api_be.Domain.DefaultValidatorBase;

namespace api_be.Domain.Models.Request.PaymentRequest
{
    public record CreateOrUpdatePaymentRequest:UpdateBaseCommand,IBasePayment
    {

        public string? InternalCode { get; set; }

        public string? Name { get; set; }
    }
}
