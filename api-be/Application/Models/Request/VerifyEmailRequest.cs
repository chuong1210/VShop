namespace api_be.Application.Models.Request
{
    public record VerifyEmailRequest
    {
        public string Token { get; set; }

    }
}
