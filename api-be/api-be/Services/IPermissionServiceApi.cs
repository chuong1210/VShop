using api_be.Models.Request;
using api_be.Models.Responses;

namespace api_be.Services
{
    public interface IPermissionServiceApi
    {
        public Task<Result<RoleDto>> AssignPermissionsToRole(AssignPermissionsToRoleRequest request);
        public  Task<Result<PermissionDto>> AddPermission(CreatePermissionRequest request);


    }
}
