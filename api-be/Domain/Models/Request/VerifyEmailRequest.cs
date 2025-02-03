namespace api_be.Domain.Models.Request
{
    public record VerifyEmailRequest
    {
        public string Token { get; set; }

    }
}
