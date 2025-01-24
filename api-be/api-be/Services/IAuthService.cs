using api_be.Entities.Auth;
using api_be.Models;
using api_be.Models.Request;
using api_be.Models.Responses;

namespace api_be.Services
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
