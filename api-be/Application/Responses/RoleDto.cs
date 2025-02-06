using api_be.Application.Models.Common;

namespace api_be.Application.Responses
{
    public record RoleDto : BaseDto
    {
        public string? Name { get; set; }

        public List<string>? Permissions { get; set; }
    }
}
