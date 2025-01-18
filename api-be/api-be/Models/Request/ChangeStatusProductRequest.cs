using static api_be.Entities.Product;

namespace api_be.Models.Request
{
    public record ChangeStatusProductRequest
    {
        public int? ProductId { get; set; }

        public ProductStatus? Status { get; set; }
    }
}
