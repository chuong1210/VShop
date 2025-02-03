using api_be.Domain.Models.Responses;
using api_be.Application.Services.Imps;
using Microsoft.Extensions.Caching.Memory;

namespace api_be.Application.Services
{
    public interface IOtpService
    {
    
        public Task<Result<bool>> SendOtp(string phoneNumber);
        public Result<bool> VerifyOtp(string phoneNumber, string otpCode);




    }

}
