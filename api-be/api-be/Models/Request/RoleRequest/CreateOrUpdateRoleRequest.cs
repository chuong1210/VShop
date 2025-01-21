using api_be.Domain.Interfaces;
using api_be.Models.ValidatorRequest.DefaultBase;

namespace api_be.Models.Request.RoleRequest
{
    public record CreateOrUpdateRoleRequest: UpdateBaseCommand, IBaseRole
    {
        public string? Name { get; set; }

        public List<string>? Permissions { get; set; }
    }
}
