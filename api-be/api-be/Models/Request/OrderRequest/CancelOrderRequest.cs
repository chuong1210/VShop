using api_be.Domain.Interfaces;

namespace api_be.Models.Request.OrderRequest
{
    public record CancelOrderRequest: IBaseOrder
    {
        public int? OrderId { get; set; }

    }
}
