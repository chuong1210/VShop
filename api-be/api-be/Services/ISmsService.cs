namespace api_be.Services
{
    public interface ISMSService
    {
        Task<bool> SendOTPAsync(string phoneNumber, string otpCode);
    }

}
