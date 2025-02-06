using api_be.Application.Models.Common;

namespace api_be.Application.Responses
{
    public record StaffPositionDto : BaseDto
    {
        public string? InternalCode { get; set; }

        public string? Name { get; set; }

        public string? Describes { get; set; }

        public List<int?>? Roles { get; set; }
    }
}
