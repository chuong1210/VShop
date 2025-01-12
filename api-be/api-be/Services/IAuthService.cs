using api_be.Models;
using api_be.Models.Request;
using api_be.Models.Responses;

namespace api_be.Services
{
    public interface IAuthService
    {
        public Task<Result<LoginDto>> Login(LoginAccountRequest request);
        public Task<Result<UserDto>> Register(RegisterAccountRequest request);


    }
}
