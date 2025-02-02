using api_be.Core.Domain;

namespace api_be.Core.Entities.Auth
{
    public class UserVerification:AuditableEntity
    {
        public int? UserId { get; set; }
        public string Token { get; set; }
        public DateTime ExpiryDate { get; set; }
        public bool IsUsed { get; set; }
        public virtual User? User { get; set; }

        public string? OTPCode { get; set; }
    }
}
