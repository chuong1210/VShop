using api_be.Application.Models.ValidatorRequest.DefaultValidatorBase;
using api_be.Core.Domain.Interfaces;
using api_be.Core.Entities.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace api_be.Application.Models.Request.SupplierOrderRequest
{
    public record CreateOrUpdateSupplierOrderRequest : UpdateBaseCommand, IBaseSupplierOrder
    {
        public int? DistributorId { get; set; }

        public List<DetailSupplierOrderDto>? Details { get; set; }
     
    }
}
