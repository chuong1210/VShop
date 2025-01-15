using api_be.Entities.Auth;
using api_be.Models;
using api_be.Models.Request;
using api_be.Models.Responses;

namespace api_be.Services
{
    public interface IAuthService
    {
        public Task<Result<LoginDto>> Login(LoginAccountRequest request);
        public Task<Result<UserDto>> Register(RegisterAccountRequest request);
        public Task<Result<UserDto>> Create(CreateUserRequest request);
        public Task<Result<UserDto>> Update(UpdateUserRequest request);
        public Task<Result<Boolean>> Delete(int userId, int currentUserId);
        public Task<Result<UserDto>> ChangePassword(ChangePasswordRequest request);



        public Task<User> ValidateTokenAsync(string request);
        public Task<Result<UserDto>> AssignRole(AssignRoleUserRequest request);
        public Task<PaginatedResult<List<UserDto>>> GetListUser(GetListUserRequest request);



    }
}
