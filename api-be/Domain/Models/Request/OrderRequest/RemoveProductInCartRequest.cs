namespace api_be.Domain.Models.Request.OrderRequest
{
    public record RemoveProductInCartRequest
    {
        public int? ProductId { get; set; }

    }
}
