using api_be.Core.Domain.Interfaces;
using api_be.Domain.DefaultValidatorBase;

namespace api_be.Domain.Models.Request.DeliveryRequest
{
    public record CreateOrUpdateDeliveryRequest :UpdateBaseCommand, IBaseDelivery
    {
        public int? OrderId { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string? From { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string? To { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public decimal? TransportFee { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    }
}
