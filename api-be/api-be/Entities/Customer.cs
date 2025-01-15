using api_be.Domain.Common;
using api_be.Entities.Auth;
using Sieve.Attributes;

namespace api_be.Domain.Entities
{
    public class Customer : AuditableEntity
    {
        [Sieve(CanFilter = true, CanSort = true)]
        public string? Name { get; set; }

        [Sieve(CanFilter = true, CanSort = true)]
        public string? Phone { get; set; }

        [Sieve(CanFilter = true, CanSort = true)]
        public string? Email { get; set; }

        [Sieve(CanFilter = true, CanSort = true)]
        public string? Address { get; set; }

        [Sieve(CanFilter = true, CanSort = true)]
        public string? Gender { get; set; }

        public virtual User? User { get; set; }
    }
}
