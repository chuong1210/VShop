using api_be.Core.Domain;
using Sieve.Attributes;

namespace api_be.Core.Entities
{
    public class Category : AuditableEntity
    {
        [Sieve(CanFilter = true, CanSort = true)]
        public string? InternalCode { get; set; }

        [Sieve(CanFilter = true, CanSort = true)]
        public string? Name { get; set; }

        public string? Icon { get; set; }


        [Sieve(CanFilter = true, CanSort = true)]
        public int? ParentId { get; set; }

        public Category? Parent { get; set; }
    }
}
