using api_be.Core.Domain.Interfaces;
using api_be.Application.Models.Request;
using api_be.Application.Models.ValidatorRequest.BaseCategory;
using api_be.Application.Models.ValidatorRequest.BaseProduct;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using static api_be.Core.Entities.Product;
using api_be.Infrastructure.DB;

namespace api_be.Application.Models.ValidatorRequest
{
    public class UpdateProductValidator : AbstractValidator<UpdateProductRequest>
    {
        public UpdateProductValidator(ISupermarketDbContext pContext, int? pCurrentId = null)
        {
            Include(new BaseProductValidator(pContext, pCurrentId));

            RuleFor(x => x.Id)
                .MustAsync(async (id, token) =>
                {
                    return await pContext.Products
                    .AnyAsync(x => x.Id == id && x.Status == ProductStatus.Draft);
                }).WithMessage("Chỉ sửa được thông tin sản phẩm khi ở trạng thái nháp!");
        }
    }
}
