using api_be.Domain.Common;
using api_be.Entities.Auth;

namespace api_be.Entities
{
    public class StaffPositionHasRole : HardDeleteEntity
    {
        public int? RoleId { get; set; }

        public Role? Role { get; set; }

        public int? StaffPositionId { get; set; }

        public StaffPosition? StaffPosition { get; set; }
    }
}
