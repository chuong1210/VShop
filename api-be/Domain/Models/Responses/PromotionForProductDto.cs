namespace api_be.Domain.Models.Responses
{
    public record PromotionForProductDto
    {
        public List<int?>? GroupProducts { get; set; }

        public int? Group { get; set; }
    }
}
