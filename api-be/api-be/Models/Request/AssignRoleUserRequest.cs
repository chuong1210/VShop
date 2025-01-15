namespace api_be.Models.Request
{
    public class AssignRoleUserRequest
    {
        public int UserId { get; set; }

        public List<int>? RolesId { get; set; }
    }
}
