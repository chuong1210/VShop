using api_be.Core.Domain.Interfaces;
using api_be.Domain.Models.Common;
using api_be.Domain.DefaultValidatorBase;

namespace api_be.Domain.Models.Request.CustomerRequest
{
    public record UpdateCustomerRequest: UpdateBaseCommand,IBaseCustomer
    {
        public string? Name { get; set; }

        public string? Phone { get; set; }

        public string? Email { get; set; }

        public string? Address { get; set; }

        public string? Gender { get; set; }
    }
}
