namespace api_be.Services
{
    public interface IEmailService
    {
        Task SendVerificationEmailAsync(string email, string verificationLink);

    }

}
