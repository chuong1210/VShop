using api_be.Domain.Common;

namespace api_be.Auth
{
    public class UserRole : HardDeleteEntity
	{
		public int? UserId { get; set; }

		public User? User { get; set; }

		public int? RoleId { get; set; }

		public Role? Role { get; set; }
	}
}
