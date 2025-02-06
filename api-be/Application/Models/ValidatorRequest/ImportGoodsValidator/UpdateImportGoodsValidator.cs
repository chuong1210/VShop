using api_be.Application.Models.Request.ImportGoodRequest;
using api_be.Application.Models.ValidatorRequest.DefaultValidatorBase;
using api_be.Application.Models.ValidatorRequest.ImportGoodsValidator.BaseImportGoods;
using api_be.Domain.Transforms;
using api_be.Infrastructure.DB;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace api_be.Application.Models.ValidatorRequest.ImportGoodsValidator
{
    internal class UpdateImportGoodsValidator:AbstractValidator<UpdateImportGoodsRequest>
    {
        public UpdateImportGoodsValidator(ISupermarketDbContext pContext, int? pSupplierOrderId)
        {
            Include(new UpdateBaseValidator<UpdateImportGoodsRequest>(pContext));
            Include(new BaseImportGoodsValidator(pContext,pSupplierOrderId));





        }
    }
}
