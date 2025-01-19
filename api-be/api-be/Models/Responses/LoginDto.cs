using api_be.Models.Common;

namespace api_be.Models.Responses
{
    public record LoginDto :BaseDto
    {

        public DateTime? Exp { get; set; }

        public string? Token { get; set; }
        public string? RefreshToken { get; set; }

    }
}
