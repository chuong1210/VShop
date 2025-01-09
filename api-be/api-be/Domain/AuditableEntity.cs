using api_be.Domain.Interfaces;
using Sieve.Attributes;

namespace api_be.Domain.Common
{
    public abstract class AuditableEntity : IAuditableEntity
    {
        public int Id { get; set; } = default!;

        [Sieve(CanFilter = true, CanSort = true)]
        public DateTime? CreatedAt { get; set; }

        [Sieve(CanFilter = true, CanSort = true)]
        public string? CreatedBy { get; set; }

        [Sieve(CanFilter = true, CanSort = true)]
        public DateTime? UpdatedAt { get; set; }

        [Sieve(CanFilter = true, CanSort = true)]
        public string? UpdatedBy { get; set; }

        [Sieve(CanFilter = true, CanSort = true)]
        public bool? IsDeleted { get; set; } = false;
    }
}
