using api_be.Core.Domain.Interfaces;
using api_be.Domain.Models.Common;
using api_be.Domain.DefaultValidatorBase;

namespace api_be.Domain.Models.Request
{
    public record UpdateProductRequest:UpdateBaseCommand, IBaseProduct
    {
        public string? InternalCode { get; set; }

        public string? Name { get; set; }

        public List<string>? Images { get; set; }

        public decimal? Price { get; set; }

        public string? Describes { get; set; }

        public string? Feature { get; set; }

        public string? Specifications { get; set; }

        // Khoá ngoại

        public int? CategoryId { get; set; }
    }
}
