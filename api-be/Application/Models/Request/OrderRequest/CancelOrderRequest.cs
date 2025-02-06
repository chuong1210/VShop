using api_be.Core.Domain.Interfaces;

namespace api_be.Application.Models.Request.OrderRequest
{
    public record CancelOrderRequest: IBaseOrder
    {
        public int? OrderId { get; set; }

    }
}
