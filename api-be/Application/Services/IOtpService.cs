using api_be.Application.Responses;
using api_be.Application.Services.Imps;
using Microsoft.Extensions.Caching.Memory;
using api_be.Domain.ResultResponses;

namespace api_be.Application.Services
{
    public interface IOtpService
    {
    
        public Task<Result<bool>> SendOtp(string phoneNumber);
        public Result<bool> VerifyOtp(string phoneNumber, string otpCode);




    }

}
