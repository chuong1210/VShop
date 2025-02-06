using api_be.Core.Domain.Interfaces;
using api_be.Application.Models.ValidatorRequest.DefaultValidatorBase;

namespace api_be.Application.Models.Request.PaymentRequest
{
    public record CreateOrUpdatePaymentRequest:UpdateBaseCommand,IBasePayment
    {

        public string? InternalCode { get; set; }

        public string? Name { get; set; }
    }
}
