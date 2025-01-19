namespace api_be.Models.Request
{
    public record ResendVerificationEmailRequest
    {
        public string Email { get; set; }

    }
}
