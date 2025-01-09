using api_be.Domain.Common;
using Sieve.Attributes;

namespace api_be.Domain.Entities
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
