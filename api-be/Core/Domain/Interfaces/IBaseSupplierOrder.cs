using api_be.Core.Entities.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace api_be.Core.Domain.Interfaces
{
    public interface IBaseSupplierOrder
    {
        public int? DistributorId { get; set; }

        public List<DetailSupplierOrderDto>? Details { get; set; }
    }
}
