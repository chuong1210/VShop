using api_be.Core.Domain;

namespace api_be.Core.Entities.Auth
{
    public class UserPermission : HardDeleteEntity
    {
        public int UserId { get; set; }

        public User? User { get; set; }

        public int PermissionId { get; set; }

        public Permission? Permission { get; set; }
    }
}
