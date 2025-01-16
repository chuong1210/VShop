using System.Net;
using System.Net.Mail;
using api_be.Models.Common;
namespace api_be.Services.Imps
{
    public class EmailService:IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly SmtpSettings _smtpSettings;
        private readonly SmtpClient _smtpClient;


        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
            //_smtpSettings = _configuration.GetSection("SmtpSettings").Get<SmtpSettings>();
            _smtpClient = new SmtpClient
            {
                Host = _configuration["EmailSettings:SmtpHost"],
                Port = int.Parse(_configuration["EmailSettings:SmtpPort"]),
                EnableSsl = true,
                Credentials = new NetworkCredential(
             _configuration["EmailSettings:SmtpUsername"],
             _configuration["EmailSettings:SmtpPassword"]
         )
            };

        }


        public async Task SendVerificationEmailAsync(string email, string verificationLink)
        {
            var mailMessage = new MailMessage
            {
                From = new MailAddress(_configuration["EmailSettings:FromEmail"]),
                Subject = "Xác nhận tài khoản của bạn",
                IsBodyHtml = true,
                Body = $@"
                <h3>Xác nhận tài khoản</h3>
                <p>Vui lòng nhấp vào liên kết dưới đây để xác nhận tài khoản của bạn:</p>
                <p><a href='{verificationLink}'>Xác nhận tài khoản</a></p>
                <p>Liên kết này sẽ hết hạn sau 24 giờ.</p>
                <p>Nếu bạn không yêu cầu xác nhận này, vui lòng bỏ qua email này.</p>"
            };
            mailMessage.To.Add(email);

            await _smtpClient.SendMailAsync(mailMessage);
        }
    }
}
