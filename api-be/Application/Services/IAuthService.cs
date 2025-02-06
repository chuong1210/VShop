using api_be.Core.Entities.Auth;
using api_be.Application.Models.Request;
using api_be.Application.Responses;
using api_be.Domain.ResultResponses;
namespace api_be.Application.Services
{
    public interface IAuthService
    {

        public Task<Result<LoginDto>> Login(LoginAccountRequest request);

        public Task<Result<LoginDto>> RefreshToken(BaseTokenRequest request);

        public Task<Result<bool>> Logout(BaseTokenRequest request);

            public Task<Result<UserDto>> Register(RegisterAccountRequest request);
  

        public Task<Result<UserDto>> ChangePassword(ChangePasswordRequest request);
        public Task<Result<bool>> ResendVerificationEmail(ResendVerificationEmailRequest request);


        public  Task<Result<bool>> ForgotPassword(ForgotPasswordRequest request);

        public Task<Result<bool>> ResetPassword(ResetPasswordRequest request);


        public Task<Result<bool>> VerifyEmail(VerifyEmailRequest request);


        public Task<User> ValidateTokenAsync(string request);
        public Task<Result<LoginDto>> ValidateGoogleToken(string token);

        public Task<Result<UserDto>> AssignRole(AssignRoleUserRequest request);



    }
}
