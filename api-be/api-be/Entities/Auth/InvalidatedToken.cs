using api_be.Domain.Common;

namespace api_be.Entities.Auth
{
    public class InvalidatedToken: HardDeleteEntity
    {
        public string JwtId { get; set; }
        public DateTime ExpiryTime { get; set; }
    }
}
