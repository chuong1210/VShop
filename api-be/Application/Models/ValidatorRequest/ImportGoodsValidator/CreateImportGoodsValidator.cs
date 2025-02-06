
using api_be.Application.Models.Request.ImportGoodRequest;
using api_be.Application.Models.ValidatorRequest.ImportGoodsValidator.BaseImportGoods;
using api_be.Domain.Transforms;
using api_be.Infrastructure.DB;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using static api_be.Core.Entities.SupplierOrder;

namespace api_be.Application.Models.ValidatorRequest.ImportGoodsValidator
{
    public class CreateImportGoodsValidator : AbstractValidator<CreateImportGoodsRequest>
    {
        public CreateImportGoodsValidator(ISupermarketDbContext pContext, int? pSupplierOrderId)
        {
            Include(new BaseImportGoodsValidator(pContext, pSupplierOrderId));

            RuleFor(x => x.SupplierOrderId)
                .MustAsync(async (supplierOrderId, token) =>
                {
                    return supplierOrderId == null ||
                    await pContext.SupplierOrders.AnyAsync(x => x.Id == supplierOrderId &&
                            (x.Status == SupplierOrderStatus.Order ||
                            x.Status == SupplierOrderStatus.PartialReceipt));
                }).WithMessage(ValidatorTransform.NotExists(Modules.SupplierOrder.DetailSupplierOrder));


        }
    }
}
