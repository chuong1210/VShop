using api_be.Application.Models.Request.SupplierOrderRequest;
using api_be.Domain.Transforms;
using api_be.Infrastructure.DB;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static api_be.Core.Entities.SupplierOrder;
namespace api_be.Application.Models.ValidatorRequest.SupllierOrderValidator
{
    public class ChangeStatusSupplierOrderValidator : AbstractValidator<ChangeStatusSupplierOrderRequest>
    {
        public ChangeStatusSupplierOrderValidator(ISupermarketDbContext pContext)
        {
            RuleFor(x => x.SupplierOrderId)
                .MustAsync(async (supplierOrderId, token) =>
                {
                    return supplierOrderId == null ||
                           await pContext.SupplierOrders.AnyAsync(x => x.Id == supplierOrderId);
                }).WithMessage(ValidatorTransform.NotExists(Modules.SupplierOrder.SupplierOrderId));

            RuleFor(x => x.Status)
                .MustAsync(async (request, status, token) =>
                {
                    var so = await pContext.SupplierOrders.FindAsync(request.SupplierOrderId);

                    if ((so.Status == SupplierOrderStatus.Draft &&
                        status == SupplierOrderStatus.Order ||
                        status == SupplierOrderStatus.Cancel) ||
                        (so.Status == SupplierOrderStatus.Order &&
                        (status == SupplierOrderStatus.Draft ||
                        status == SupplierOrderStatus.Cancel)))
                    {
                        return true;
                    }

                    return false;
                }).WithMessage("Trạng thái của danh sách sản phẩm nhập không hợp lệ!");
        }
    }

}
