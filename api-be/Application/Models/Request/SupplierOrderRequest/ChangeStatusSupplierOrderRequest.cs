using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static api_be.Core.Entities.SupplierOrder;

namespace api_be.Application.Models.Request.SupplierOrderRequest
{
    public record ChangeStatusSupplierOrderRequest
    {
        public int? SupplierOrderId { get; set; }

        public SupplierOrderStatus? Status { get; set; }
    }
}
