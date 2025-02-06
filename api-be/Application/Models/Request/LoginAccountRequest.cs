
namespace api_be.Application.Models.Request
{
    public record LoginAccountRequest
    {
        public string? UserName { get; set; }


        public string? Password { get; set; }
        public string? ExternalAccessToken { get; set; }
    }
}
