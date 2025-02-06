namespace api_be.Application.Models.Request
{
    public record BaseTokenRequest
    {
        public string ? AccessToken { get; set; }

        public string ? RefreshToken { get; set; }
    }
}
