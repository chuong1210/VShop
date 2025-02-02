using static api_be.Core.Entities.Order;

namespace api_be.Domain.Models.Request.OrderRequest
{
    public record ChangeStatusOrderRequest
    {
        public int? OrderId { get; set; }

        public OrderStatus? Status { get; set; }
    }
}
