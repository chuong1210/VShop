using api_be.Domain.Common;

namespace api_be.Auth
{
    public class UserPermission : HardDeleteEntity
    {
        public int UserId { get; set; }

        public User? User { get; set; }

        public int PermissionId { get; set; }

        public Permission? Permission { get; set; }
    }
}
