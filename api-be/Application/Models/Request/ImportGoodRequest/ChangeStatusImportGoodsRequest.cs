using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace api_be.Application.Models.Request.ImportGoodRequest
{
    public record ChangeStatusImportGoodsRequest
    {
        public int? SupplierOrderId { get; set; }

        public bool? IsCancel { get; set; }

    }
}
