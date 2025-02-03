using api_be.Core.Domain.Interfaces;
using api_be.Domain.DefaultValidatorBase;

namespace api_be.Domain.Models.Request.RoleRequest
{
    public record CreateOrUpdateRoleRequest: UpdateBaseCommand, IBaseRole
    {
        public string? Name { get; set; }

        public List<string>? Permissions { get; set; }
    }
}
