using api_be.Application.Models.Request;
using api_be.Application.Responses;
using api_be.Domain.ResultResponses;

namespace api_be.Application.Services
{
    public interface IPermissionServiceApi
    {
        public Task<Result<RoleDto>> AssignPermissionsToRole(AssignPermissionsToRoleRequest request);
        public  Task<Result<PermissionDto>> AddPermission(CreatePermissionRequest request);


    }
}
