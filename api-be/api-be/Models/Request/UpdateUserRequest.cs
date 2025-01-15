using api_be.Domain.Interfaces;

namespace api_be.Models.Request
{
    public class UpdateUserRequest:IBaseUser
    {
        public string? Email { get; set; }

        public string? PhoneNumber { get; set; }
        public int? Id { get; set; }
        public string? Password { get; set; }


    }
}
