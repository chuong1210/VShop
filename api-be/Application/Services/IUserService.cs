using api_be.Application.Models.Request;
using api_be.Application.Responses;
using api_be.Domain.ResultResponses;

namespace api_be.Application.Services
{
    public interface IUserService
    {
        public Task<Result<UserDto>> Create(CreateUserRequest request);
        public Task<Result<UserDto>> Update(UpdateUserRequest request);
        public Task<Result<Boolean>> Delete(int userId, int currentUserId);

        public Task<Result<UserDto>> Detail(int id);

        public Task<PaginatedResult<List<UserDto>>> GetListUser(GetListUserRequest request);
        public Task<Result<UserDto>> GetUserHasAdminRoleAndChatMessagePermission( );

    }
}
