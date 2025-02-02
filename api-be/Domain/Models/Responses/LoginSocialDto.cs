using api_be.Domain.Models.Common;
using Newtonsoft.Json;

namespace api_be.Domain.Models.Responses
{
    public record LoginSocialDto: BaseDto
    {
        [JsonProperty("email")]
        public string? Email { get; set; }

        public string? FirstName { get; set; }

        public string? LastName { get; set; }
    }
}
