namespace api_be.Application.Responses
{
    public record PromotionForProductDto
    {
        public List<int?>? GroupProducts { get; set; }

        public int? Group { get; set; }
    }
}
