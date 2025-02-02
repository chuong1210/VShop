using api_be.Core.Domain.Interfaces;

namespace api_be.Domain.Models.Request
{
    public record CreateUserRequest:IBaseUser
    {
        public string? UserName { get; set; }

        public string? Password { get; set; }

        public string? Email { get; set; }

        public string? PhoneNumber { get; set; }
    }
}
