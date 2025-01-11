
using api_be.Domain.Common;

namespace api_be.Auth
{
    public class RolePermission : HardDeleteEntity
	{
		public int RoleId { get; set; }

		public Role? Role { get; set; }

		public int PermissionId { get; set; }

		public Permission? Permission { get; set; }
	}
}
