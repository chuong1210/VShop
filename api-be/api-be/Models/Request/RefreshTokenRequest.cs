namespace api_be.Models.Request
{
    public record RefreshTokenRequest
    {
        public string ? AccessToken { get; set; }
        public string ? RefreshToken { get; set; }
    }
}
