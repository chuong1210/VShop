using api_be.Core.Domain.Interfaces;
using api_be.Domain.DefaultValidatorBase;

namespace  api_be.Domain.Models.Request.DistributorRequest 
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
