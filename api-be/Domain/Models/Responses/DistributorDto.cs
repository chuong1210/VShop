using api_be.Domain.Models.Common;

namespace api_be.Domain.Models.Responses
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
