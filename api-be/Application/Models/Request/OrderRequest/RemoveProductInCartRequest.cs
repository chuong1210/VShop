namespace api_be.Application.Models.Request.OrderRequest
{
    public record RemoveProductInCartRequest
    {
        public int? ProductId { get; set; }

    }
}
