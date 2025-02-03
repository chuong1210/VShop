using api_be.Core.Domain;
using Sieve.Attributes;

namespace api_be.Core.Entities.Auth
{
    public class Role : AuditableEntity
    {
        [Sieve(CanFilter = true, CanSort = true)]
        public string? Name { get; set; }

        public List<UserRole>? UserRoles { get; set; }

        public List<RolePermission>? RolePermissions { get; set; }
    }
}
