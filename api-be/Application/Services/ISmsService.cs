namespace api_be.Application.Services
{
    public interface ISMSService
    {
        Task<bool> SendOTPAsync(string phoneNumber, string otpCode);
    }

}
