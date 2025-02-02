using api_be.Domain.Models.Common;

namespace api_be.Domain.Models.Responses
{
    public record StaffPositionDto : BaseDto
    {
        public string? InternalCode { get; set; }

        public string? Name { get; set; }

        public string? Describes { get; set; }

        public List<int?>? Roles { get; set; }
    }
}
