namespace api_be.Models.Responses
{
    public record PromotionForProductDto
    {
        public List<int?>? GroupProducts { get; set; }

        public int? Group { get; set; }
    }
}
