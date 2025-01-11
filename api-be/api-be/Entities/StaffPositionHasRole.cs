using api_be.Auth;
using api_be.Domain.Common;

namespace api_be.Domain.Entities
{
    public class StaffPositionHasRole : HardDeleteEntity
    {
        public int? RoleId { get; set; }

        public Role? Role { get; set; }

        public int? StaffPositionId { get; set; }

        public StaffPosition? StaffPosition { get; set; }
    }
}
