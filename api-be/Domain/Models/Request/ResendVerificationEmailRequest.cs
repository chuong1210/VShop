namespace api_be.Domain.Models.Request
{
    public record ResendVerificationEmailRequest
    {
        public string Email { get; set; }

    }
}
