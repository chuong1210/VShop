using api_be.Application.Models.Request.SupplierOrderRequest;
using api_be.Application.Models.ValidatorRequest.DefaultValidatorBase;
using api_be.Application.Models.ValidatorRequest.SupllierOrderValidator.BaseSupplierOrder;
using api_be.Infrastructure.DB;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace api_be.Application.Models.ValidatorRequest.SupllierOrderValidator
{
    public class CreateOrUpdateSupplierOrderValidator:AbstractValidator<CreateOrUpdateSupplierOrderRequest>
    {
        public CreateOrUpdateSupplierOrderValidator(ISupermarketDbContext pContext, int? pSupplierOrderId)
        {
            Include(new BaseSupplierOrderValidator(pContext));
            if (pSupplierOrderId == null)
            {


                Include(new UpdateBaseValidator<CreateOrUpdateSupplierOrderRequest>(pContext));
            }
            }
        }
}
