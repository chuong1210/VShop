using api_be.Application.Models.Request;
using api_be.Application.Models.Request.RoleRequest;
using api_be.Application.Responses;
using api_be.Domain.ResultResponses;
using api_be.Application.Models.ValidatorRequest.DefaultValidatorBase;


namespace api_be.Application.Services
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
