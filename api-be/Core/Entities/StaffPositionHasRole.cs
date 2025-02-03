using api_be.Core.Domain;
using api_be.Core.Entities.Auth;

namespace api_be.Core.Entities
{
    public class StaffPositionHasRole : HardDeleteEntity
    {
        public int? RoleId { get; set; }

        public Role? Role { get; set; }

        public int? StaffPositionId { get; set; }

        public StaffPosition? StaffPosition { get; set; }
    }
}
