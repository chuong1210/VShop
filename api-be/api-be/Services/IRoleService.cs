using api_be.Models.Request;
using api_be.Models.Request.RoleRequest;
using api_be.Models.Responses;
using api_be.Models.ValidatorRequest.DefaultBase;
using api_be.ValidatorRequest.DefaultBase;

namespace api_be.Services
{
    public interface IRoleService
    {
        public Task<Result<RoleDto>> Create(CreateOrUpdateRoleRequest request);
        public Task<Result<RoleDto>> AssignPermissionsForRole(AssignPermissionsForRoleRequest request);
        public Task<Result<RoleDto>> Update(CreateOrUpdateRoleRequest request);
        public Task<Result<Boolean>> Delete(int id);
        public Task<Result<RoleDto>> Detail(DetailBaseCommand request);
        public Task<PaginatedResult<List<RoleDto>>> GetList(ListBaseCommand request);
        public Task<PaginatedResult<List<RoleDto>>> GetListRoleWithPermission(ListBaseCommand request);

    }
}
