
namespace api_be.Models.Request
{
    public record LoginAccountRequest
    {
        public string? UserName { get; set; }
        public string? Email { get; set; }


        public string? Password { get; set; }
    }
}
