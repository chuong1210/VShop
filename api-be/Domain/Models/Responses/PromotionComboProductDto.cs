
namespace api_be.Domain.Models.Responses
{
    public record PromotionComboProductDto
    {
        public int? Id { get; set; }

        public List<ProductDto>? Products { get; set; }

        public PromotionDto? Promotion { get; set; }

        public decimal? Price { get; set; }

        public decimal? ReducedPrice { get; set; }

        public decimal? NewPrice { get; set; }
    }
}
