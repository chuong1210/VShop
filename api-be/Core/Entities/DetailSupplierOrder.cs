using api_be.Core.Domain;

namespace api_be.Core.Entities
{
    public class DetailSupplierOrder : HardDeleteEntity
    {
        public decimal? Price { get; set; }

        public int? Quantity { get; set; }

        // Khoá ngoại
        public int? SupplierOrderId { get; set; }
        public SupplierOrder? SupplierOrder { get; set; }

        public int? ProductId { get; set; }
        public Product? Product { get; set; }
    }
}
