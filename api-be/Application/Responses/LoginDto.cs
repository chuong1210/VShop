using api_be.Application.Models.Common;

namespace api_be.Application.Responses
{
    public record LoginDto :BaseDto
    {

        public DateTime? Exp { get; set; }

        public string? Token { get; set; }
        public int? userId { get; set; }

        public string? RefreshToken { get; set; }

    }
}
