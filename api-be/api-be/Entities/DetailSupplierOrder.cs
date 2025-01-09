﻿using api_be.Domain.Common;

namespace api_be.Domain.Entities
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
