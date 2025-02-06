using api_be.Core.Domain.Interfaces;
using api_be.Application.Models.Common;
using api_be.Application.Models.ValidatorRequest.DefaultValidatorBase;

namespace api_be.Application.Models.Request.CustomerRequest
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
