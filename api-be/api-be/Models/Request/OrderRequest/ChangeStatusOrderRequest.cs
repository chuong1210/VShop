using static api_be.Entities.Order;

namespace api_be.Models.Request.OrderRequest
{
    public record ChangeStatusOrderRequest
    {
        public int? OrderId { get; set; }

        public OrderStatus? Status { get; set; }
    }
}
