using static api_be.Core.Entities.Product;

namespace api_be.Application.Models.Request
{
    public record ChangeStatusProductRequest
    {
        public int? ProductId { get; set; }

        public ProductStatus? Status { get; set; }
    }
}
