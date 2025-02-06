using api_be.Application.Models.Common;

namespace api_be.Application.Responses
{
    public record DistributorDto : BaseDto
    {
        public string? InternalCode { get; set; }

        public string? Name { get; set; }

        public string? Email { get; set; }

        public string? Phone { get; set; }

        public string? Address { get; set; }
    }
}
