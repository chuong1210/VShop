using api_be.Domain.Interfaces;
using api_be.Models.Common;
using api_be.Models.ValidatorRequest.DefaultBase;

namespace api_be.Models.Request.CustomerRequest
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
