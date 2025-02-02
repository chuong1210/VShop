namespace api_be.Domain.Models.Request
{
    public record BaseTokenRequest
    {
        public string ? AccessToken { get; set; }

        public string ? RefreshToken { get; set; }
    }
}
