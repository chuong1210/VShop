using api_be.Core.Domain.Interfaces;
using api_be.Domain.Models.Request;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using static api_be.Core.Entities.Product;
using api_be.Infrastructure.DB;

namespace api_be.Application.ValidatorRequest
{

    public class ChangeStatusProductValidator : AbstractValidator<ChangeStatusProductRequest>
    {
        public ChangeStatusProductValidator(ISupermarketDbContext pContext)
        {
            RuleFor(x => x.ProductId)
                .MustAsync(async (ProductId, token) =>
                {
                    return await pContext.Products
                            .AnyAsync(x => x.Id == ProductId &&
                                           x.Type == ProductType.Option);
                }).WithMessage("Id sản phẩm không hợp lệ!");

            RuleFor(x => x.Status)
                .MustAsync(async (request, status, token) =>
                {
                    var product = await pContext.Products.FindAsync(request.ProductId);

                    //if (!((product.Status == ProductStatus.Draft &&
                    //   (status == ProductStatus.Active ||
                    //    status == ProductStatus.Pause ||
                    //    status == ProductStatus.Stop)) ||
                    //    (product.Status == ProductStatus.Active &&
                    //   (status == ProductStatus.Draft ||
                    //    status == ProductStatus.Pause ||
                    //    status == ProductStatus.Stop)) ||
                    //    (product.Status == ProductStatus.Pause &&
                    //   (status == ProductStatus.Draft ||
                    //    status == ProductStatus.Active ||
                    //    status == ProductStatus.Stop))))
                    //{
                    //    return false;
                    //}

                    //return true;
                    return product.Status switch
                    {
                        ProductStatus.Draft => status != ProductStatus.Active && status != ProductStatus.Pause && status != ProductStatus.Stop,
                        ProductStatus.Active => status != ProductStatus.Draft && status != ProductStatus.Pause && status != ProductStatus.Stop,
                        ProductStatus.Pause => status != ProductStatus.Draft && status != ProductStatus.Active && status != ProductStatus.Stop,
                        _ => true
                    };

                }).WithMessage("Trạng thái thay đổi sản phẩm không hợp lệ!");
        }
    }
    }
