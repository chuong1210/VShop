using api_be.Core.Domain.Interfaces;

namespace api_be.Domain.Models.Request
{
    public record UpdateUserRequest:IBaseUser
    {
        public string? Email { get; set; }

        public string? PhoneNumber { get; set; }
        public int? Id { get; set; }
        public string? Password { get; set; }


    }
}
