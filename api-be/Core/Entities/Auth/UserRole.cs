using api_be.Core.Domain;

namespace api_be.Core.Entities.Auth
{
    public class UserRole : HardDeleteEntity
    {
        public int? UserId { get; set; }

        public User? User { get; set; }

        public int? RoleId { get; set; }

        public Role? Role { get; set; }
    }
}
