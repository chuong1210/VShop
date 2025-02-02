namespace api_be.Domain.Models.Request
{
    public record ForgotPasswordRequest
    {
        public string? Email { get; set; }
    }

    public record ResetPasswordRequest
    {
        public string? Token { get; set; }
        public string? NewPassword { get; set; }
        public string ?ConfirmPassword { get; set; }
    }
}
