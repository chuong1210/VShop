using api_be.Core.Domain;
using Sieve.Attributes;

namespace api_be.Core.Entities
{
    public class Distributor : AuditableEntity
    {
        [Sieve(CanFilter = true, CanSort = true)]
        public string? InternalCode { get; set; }

        [Sieve(CanFilter = true, CanSort = true)]
        public string? Name { get; set; }

        [Sieve(CanFilter = true, CanSort = true)]
        public string? Email { get; set; }

        [Sieve(CanFilter = true, CanSort = true)]
        public string? Phone { get; set; }

        public string? Address { get; set; }
    }
}
