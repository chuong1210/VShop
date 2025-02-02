namespace api_be.Domain.Models.Request.OrderRequest
{
    public record AddProductToCartRequest
    {
        public int? ProductId { get; set; }

        public int? Quantity { get; set; }
    }
}
