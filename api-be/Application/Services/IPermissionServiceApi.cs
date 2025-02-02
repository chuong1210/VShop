using api_be.Domain.Models.Request;
using api_be.Domain.Models.Responses;

namespace api_be.Application.Services
{
    public interface IPermissionServiceApi
    {
        public Task<Result<RoleDto>> AssignPermissionsToRole(AssignPermissionsToRoleRequest request);
        public  Task<Result<PermissionDto>> AddPermission(CreatePermissionRequest request);


    }
}
