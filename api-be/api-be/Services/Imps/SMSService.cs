
using Twilio;
using Twilio.Types;

namespace api_be.Services.Imps
{
    public class SMSService : ISMSService
    {
        private readonly IConfiguration _configuration;
        private readonly TwilioClient _twilioClient;

        public SMSService(IConfiguration configuration)
        {
            _configuration = configuration;

            // Initialize Twilio client
            string accountSid = _configuration["Twilio:AccountSid"];
            string authToken = _configuration["Twilio:AuthToken"];
            TwilioClient.Init(accountSid, authToken);
        }
        public async Task<bool> SendOTPAsync(string phoneNumber, string otpCode)
        {
            try
            {
                //var message = await MessageResource.CreateAsync(
                //    body: $"Mã OTP xác thực của bạn là: {otpCode}. Mã có hiệu lực trong 5 phút.",
                //    from: new PhoneNumber(_configuration["Twilio:PhoneNumber"]),
                //    to: new PhoneNumber(phoneNumber)
                //);

                //return message.Status != MessageStatus.Failed;
                return true;

            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
