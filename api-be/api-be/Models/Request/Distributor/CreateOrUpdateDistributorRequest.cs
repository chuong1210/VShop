using api_be.Domain.Interfaces;
using api_be.Models.ValidatorRequest.DefaultBase;

namespace api_be.Models.Request.Distributor
{
    public record CreateOrUpdateDistributorRequest:UpdateBaseCommand,IBaseDistributor
    {
        public string? InternalCode { get; set; }

        public string? Name { get; set; }

        public string? Email { get; set; }

        public string? Phone { get; set; }

        public string? Address { get; set; }
    }
}
