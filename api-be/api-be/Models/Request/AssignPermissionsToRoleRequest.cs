namespace api_be.Models.Request
{
    public record AssignPermissionsToRoleRequest
    {
        public int? RoleId { get; set; }
        public List<string> Permissions { get; set; } = new List<string>();

    }
}
