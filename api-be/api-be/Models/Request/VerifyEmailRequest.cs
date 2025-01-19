namespace api_be.Models.Request
{
    public record VerifyEmailRequest
    {
        public string Token { get; set; }

    }
}
