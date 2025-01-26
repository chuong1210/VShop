using api_be.Domain.Interfaces;
using api_be.Models.ValidatorRequest.DefaultBase;

namespace api_be.Models.Request.DeliveryRequest
{
    public record CreateOrUpdateDeliveryRequest :UpdateBaseCommand, IBaseDelivery
    {
        public int? OrderId { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string? From { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string? To { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public decimal? TransportFee { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    }
}
