using api_be.Models.Common;
using static api_be.Entities.Auth.User;
using api_be.Domain.Interfaces;
using Newtonsoft.Json;
namespace api_be.Models.Responses
{
    public record UserDto : BaseDto, IBaseUser
    {
        public string? UserName { get; set; }

        [JsonProperty("email")]

        public string? Email { get; set; }
        [JsonProperty("PhoneNumber")]

        public string? PhoneNumber { get; set; }

        public UserType? Type { get; set; }

        public List<string>? Roles { get; set; }

        public int? StaffId { get; set; }

        public StaffDto? Staff { get; set; }

        public int? CustomerId { get; set; }

        public CustomerDto? Customer { get; set; }
    }
}
