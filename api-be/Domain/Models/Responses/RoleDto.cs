using api_be.Domain.Models.Common;

namespace api_be.Domain.Models.Responses
{
    public record RoleDto : BaseDto
    {
        public string? Name { get; set; }

        public List<string>? Permissions { get; set; }
    }
}
