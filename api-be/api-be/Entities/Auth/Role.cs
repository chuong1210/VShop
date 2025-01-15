using api_be.Domain.Common;
using Sieve.Attributes;

namespace api_be.Entities.Auth
{
    public class Role : AuditableEntity
    {
        [Sieve(CanFilter = true, CanSort = true)]
        public string? Name { get; set; }

        public List<UserRole>? UserRoles { get; set; }

        public List<RolePermission>? RolePermissions { get; set; }
    }
}
