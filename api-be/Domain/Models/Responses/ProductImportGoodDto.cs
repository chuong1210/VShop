
using api_be.Domain.Models;
using api_be.Domain.Models.Common;
using api_be.Domain.Models.Responses;
using static api_be.Core.Entities.Product;

namespace api_be.Domain.Models.Responses
{
    public record ProductImportGoodDto : BaseDto
    {
        public string? InternalCode { get; set; }

        public string? Name { get; set; }

        public List<string>? Images { get; set; }

        public decimal? Price { get; set; }

        public int? Quantity { get; set; }

        public string? Describes { get; set; }

        public string? Feature { get; set; }

        public string? Specifications { get; set; }

        public ProductStatus? Status { get; set; }

        public CategoryDto? Category { get; set; }

        public int? OrderQuantity { get; set; }

        public int? ImportQuantity { get; set; }
    }
}
