namespace api_be.Application.Models.Request.OrderRequest
{
    public record UpdateProductInCartRequest
    {
        public int? ProductId { get; set; }

        public int? Quantity { get; set; }

        public bool? IsSelected { get; set; }
    }
}
