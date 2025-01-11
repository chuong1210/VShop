using api_be.Domain.Common;
using Sieve.Attributes;

namespace api_be.Auth
{
    public class Permission : HardDeleteEntity
	{
        [Sieve(CanFilter = true, CanSort = true)]
        public string? Name { get; set; }

		public List<RolePermission>? RolePermissions { get; set; }

		public List<UserPermission>? UserPermissions { get; set; }
	}
}
