using api_be.Domain.ResultResponses;

namespace api_be.Application.Services
{
    public interface IEmailService
    {
        Task SendVerificationEmailAsync(string email, string verificationLink);
        Task SendPasswordResetEmailAsync(string email, string resetLink);
        Task SendMailResetPasswordAsycn(string email);


    }

}
