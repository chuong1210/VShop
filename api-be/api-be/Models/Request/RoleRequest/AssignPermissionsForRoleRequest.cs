namespace api_be.Models.Request.RoleRequest
{
    public record AssignPermissionsForRoleRequest
    {
        public int RoleId { get; set; }

        public List<string>? PermissionsName { get; set; }

        public List<string>? AddPermission { get; set; }

        public List<string>? DeletePermission { get; set; }
    }
}
