using api_be.Models.Common;
using Newtonsoft.Json;

namespace api_be.Models.Responses
{
    public record LoginSocialDto: BaseDto
    {
        [JsonProperty("email")]
        public string? Email { get; set; }

        public string? FirstName { get; set; }

        public string? LastName { get; set; }
    }
}
