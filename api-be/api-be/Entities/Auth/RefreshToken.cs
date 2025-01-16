using api_be.Domain.Common;

namespace api_be.Entities.Auth
{
    public class RefreshToken: AuditableEntity
    {
        public int UserId { get; set; }
        public string Token { get; set; }
        public DateTime ExpiryDate { get; set; }
        public bool IsUsed { get; set; }
        public bool IsRevoked { get; set; }
        public virtual User User { get; set; }
    }
}
