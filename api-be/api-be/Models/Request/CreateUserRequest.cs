using api_be.Domain.Interfaces;

namespace api_be.Models.Request
{
    public class CreateUserRequest:IBaseUser
    {
        public string? UserName { get; set; }

        public string? Password { get; set; }

        public string? Email { get; set; }

        public string? PhoneNumber { get; set; }
    }
}
