using api_be.Domain.Models.Common;

namespace api_be.Domain.Models.Responses
{
    public record CategoryDto : BaseDto
    {
        public string? InternalCode { get; set; }

        public string? Name { get; set; }

        public string? Icon { get; set; }

        public int? ParentId { get; set; }

        public CategoryDto? Parent { get; set; }
    }
}
