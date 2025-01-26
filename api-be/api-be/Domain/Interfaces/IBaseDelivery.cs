namespace api_be.Domain.Interfaces
{
    public interface IBaseDelivery
    {
        int? OrderId { get; set; }

        string? From { get; set; }

        string? To { get; set; }

        decimal? TransportFee { get; set; }
    }
}
