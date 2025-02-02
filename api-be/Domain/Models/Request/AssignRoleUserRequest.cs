namespace api_be.Domain.Models.Request
{
    public record AssignRoleUserRequest
    {
        public int UserId { get; set; }

        public List<int>? RolesId { get; set; }
    }
}
