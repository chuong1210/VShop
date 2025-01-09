﻿using api_be.Models.Common;
using api_be.Domain.Entities;
using static api_be.Domain.Entities.Product;

namespace api_be.Models
{
    public record ProductDto : BaseDto
    {
        public string? InternalCode { get; set; }

        public string? Name { get; set; }

        public List<string>? Images { get; set; }

        public decimal? Price { get; set; }

        public decimal? NewPrice { get; set; }

        public int? Quantity { get; set; }

        public string? Describes { get; set; }

        public string? Feature { get; set; }

        public string? Specifications { get; set; }


        public ProductStatus? Status { get; set; }

        public int? ParentId { get; set; }

        public ProductDto? Parent { get; set; }

        public int? CategoryId { get; set; }

        public CategoryDto? Category { get; set; }

        public PromotionDto? PromotionDto { get; set; }
    }
}
