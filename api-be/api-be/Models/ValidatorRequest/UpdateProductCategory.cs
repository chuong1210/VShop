using api_be.Domain.Interfaces;
using api_be.Models.Request;
using api_be.ValidatorRequest.BaseCategory;
using api_be.ValidatorRequest.BaseProduct;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using static api_be.Entities.Product;
namespace api_be.Models.ValidatorRequest
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
