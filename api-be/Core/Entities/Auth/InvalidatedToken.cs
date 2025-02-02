using api_be.Core.Domain;

namespace api_be.Core.Entities.Auth
{
    public class InvalidatedToken: HardDeleteEntity
    {
        public string JwtId { get; set; }
        public DateTime ExpiryTime { get; set; }
    }
}
