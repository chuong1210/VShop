using api_be.Application.Models.ValidatorRequest.DefaultValidatorBase;
using api_be.Core.Domain.Interfaces;
using api_be.Core.Entities.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace api_be.Application.Models.Request.ImportGoodRequest
{
    public record CreateImportGoodsRequest: IBaseImportGoods
    {
        public int? SupplierOrderId { get; set; }

        public string? ReceivingStaff { get; set; }

        public List<DetailImportGoodDto>? Details { get; set; }
    }

}
