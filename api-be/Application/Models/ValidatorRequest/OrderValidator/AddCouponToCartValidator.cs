using api_be.Core.Domain.Interfaces;
using api_be.Application.Models.Request.OrderRequest;
using static api_be.Core.Entities.Coupon;
using api_be.Infrastructure.DB;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace api_be.Application.Models.ValidatorRequest.OrderValidator
{
    public class AddCouponToCartValidator : AbstractValidator<AddCouponToCartRequest>
    {
        public AddCouponToCartValidator(ISupermarketDbContext pContext, int? pCustomerId)
        {
            RuleFor(x => x.InternalCodeCoupon)
                .MustAsync(async (code, token) =>
                {
                    return await pContext.Coupons
                    .AnyAsync(x => x.InternalCode == code &&
                                x.Start <= DateTime.Now && DateTime.Now <= x.End &&
                                x.Status == CouponStatus.Approve &&
                                x.Limit > 0 &&
                                (x.TypeC == CType.MC ||
                                x.TypeC == CType.SC &&
                                x.CustomerId == pCustomerId));
                }).WithMessage("Mã chương trình khuyến mãi không hợp lệ!");
        }
    }
}
