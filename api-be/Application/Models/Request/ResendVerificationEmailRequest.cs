namespace api_be.Application.Models.Request
{
    public record ResendVerificationEmailRequest
    {
        public string Email { get; set; }

    }
}
