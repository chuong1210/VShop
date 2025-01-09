using api_be.Models.Common;

namespace api_be.Models
{
    public record RoleDto : BaseDto
    {
        public string? Name { get; set; }

        public List<string>? Permissions { get; set; }
    }
}
