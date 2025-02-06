using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace api_be.Core.Entities.Dto
{
    public record DetailSupplierOrderDto
    {
        public decimal? Price { get; set; }

        public int? Quantity { get; set; }

        public int? ProductId { get; set; }
    }
}
