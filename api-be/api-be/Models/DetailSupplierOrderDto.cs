using api_be.Models.Common;

namespace api_be.Models
{
    public record DetailSupplierOrderDto : BaseDto
    {
        public decimal? Price { get; set; }

        public int? Quantity { get; set; }

        public int? ProductId { get; set; }

        public ProductDto? Product { get; set; }
    }
}
