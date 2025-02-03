using api_be.Core.Domain;

namespace api_be.Core.Entities.Auth
{
    public class RolePermission : HardDeleteEntity
    {
        public int RoleId { get; set; }

        public Role? Role { get; set; }

        public int PermissionId { get; set; }

        public Permission? Permission { get; set; }
    }
}
