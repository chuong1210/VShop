using Sieve.Attributes;
using api_be.Core.Domain;
using api_be.Core.Domain.Interfaces;
namespace api_be.Core.Domain
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
