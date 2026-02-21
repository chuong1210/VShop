using api_be.Core.Domain.Interfaces;
using api_be.Application.Models.Request;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using static api_be.Core.Entities.Product;
using api_be.Infrastructure.DB;

namespace api_be.Application.Models.ValidatorRequest
{
    public class ChangeStatusProductValidator : AbstractValidator<ChangeStatusProductRequest>
    {
        public ChangeStatusProductValidator(ISupermarketDbContext pContext)
        {
            RuleFor(x => x.ProductId)
                .NotNull().WithMessage("ProductId không được để trống!")
                .MustAsync(async (ProductId, token) =>
                {
                    return await pContext.Products
                            .AnyAsync(x => x.Id == ProductId && x.Type == ProductType.Option, token);
                }).WithMessage("Id sản phẩm không hợp lệ!");

            RuleFor(x => x.Status)
                .NotNull().WithMessage("Status không được để trống!")
                .MustAsync(async (request, status, token) =>
                {
                    var product = await pContext.Products.FindAsync(new object[] { request.ProductId }, token);

                    if (product == null) return false;

                    // Logic chuyển trạng thái hợp lệ
                    return product.Status switch
                    {
                        // Từ Draft có thể chuyển sang: Active, Pause, Stop
                        ProductStatus.Draft => status == ProductStatus.Active ||
                                              status == ProductStatus.Pause ||
                                              status == ProductStatus.Stop,

                        // Từ Active có thể chuyển sang: Draft, Pause, OutStock, Stop
                        ProductStatus.Active => status == ProductStatus.Draft ||
                                               status == ProductStatus.Pause ||
                                               status == ProductStatus.OutStock ||
                                               status == ProductStatus.Stop,

                        // Từ Pause có thể chuyển sang: Draft, Active, Stop
                        ProductStatus.Pause => status == ProductStatus.Draft ||
                                              status == ProductStatus.Active ||
                                              status == ProductStatus.Stop,

                        // Từ OutStock có thể chuyển sang: Draft, Active, Stop
                        ProductStatus.OutStock => status == ProductStatus.Draft ||
                                                 status == ProductStatus.Active ||
                                                 status == ProductStatus.Stop,

                        // Từ Stop có thể chuyển sang: Draft (để chỉnh sửa lại)
                        ProductStatus.Stop => status == ProductStatus.Draft,

                        _ => false
                    };
                }).WithMessage("Trạng thái thay đổi sản phẩm không hợp lệ!");
        }
    }
}