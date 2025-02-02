using api_be.Core.Domain.Interfaces;
using api_be.Domain.Models.Responses;

namespace api_be.Domain.Models.Request
{
    public record RegisterAccountRequest:IBaseUser
    {
            public string? Name { get; set; }

            public string? UserName { get; set; }

            public string? Password { get; set; }

            public string? Email { get; set; }

            public string? PhoneNumber { get; set; }

            public string? Address { get; set; }

            public string? Gender { get; set; }
    }
}
