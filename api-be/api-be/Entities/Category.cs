﻿using api_be.Domain.Common;
using Sieve.Attributes;

namespace api_be.Domain.Entities
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
