namespace api_be.Domain.Models.Request.PromotionRequest
{
    public record ApplyPromotionForProductRequest
    {
        public int? PromotionId { get; set; }

        public List<int>? ProductsId { get; set; }

        public int? Group { get; set; }
    }
}
