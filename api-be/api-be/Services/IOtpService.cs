using api_be.Models.Responses;
using api_be.Services.Imps;
using Microsoft.Extensions.Caching.Memory;

namespace api_be.Services
{
    public interface IOtpService
    {
    
        public Task<Result<bool>> SendOtp(string phoneNumber);
        public Result<bool> VerifyOtp(string phoneNumber, string otpCode);




    }

}
