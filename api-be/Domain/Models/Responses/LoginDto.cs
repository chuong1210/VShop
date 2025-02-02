using api_be.Domain.Models.Common;

namespace api_be.Domain.Models.Responses
{
    public record LoginDto :BaseDto
    {

        public DateTime? Exp { get; set; }

        public string? Token { get; set; }
        public string? RefreshToken { get; set; }

    }
}
