namespace api_be.Models.Request.OrderRequest
{
    public record RemoveProductInCartRequest
    {
        public int? ProductId { get; set; }

    }
}
