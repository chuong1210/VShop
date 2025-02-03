using api_be.Core.Domain;
using Sieve.Attributes;

namespace api_be.Core.Entities
{
    public class StaffPosition : AuditableEntity
    {
        [Sieve(CanFilter = true, CanSort = true)]
        public string? InternalCode { get; set; }

        [Sieve(CanFilter = true, CanSort = true)]
        public string? Name { get; set; }

        public string? Describes { get; set; }
    }
}
