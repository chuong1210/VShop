using api_be.Domain.Interfaces;
using api_be.Models.Request.CouponRequest;
using api_be.Transforms;
using static api_be.Entities.Coupon;

namespace api_be.Models.ValidatorRequest.CouponValidator
{

    public class ChangeStatusCouponValidator : AbstractValidator<ChangeStatusCouponRequest>
    {
        public ChangeStatusCouponValidator(ISupermarketDbContext pContext)
        {
            RuleFor(x => x.CouponId)
                   .MustAsync(async (couponId, token) =>
                   {
                       return couponId == null ||
                       await pContext.Coupons.AnyAsync(x => x.Id == couponId && x.IsDeleted == false);
                   }).WithMessage(ValidatorTransform.NotExists(Modules.Coupon.Id));

            var enumValues = Enum.GetValues(typeof(CouponStatus))
                    .Cast<CouponStatus>()
                    .Select(v => v.ToString())
                    .ToArray();

            RuleFor(x => x.Status)
                .IsInEnum()
                .WithMessage(ValidatorTransform.Must(Modules.Coupon.Status, string.Join(", ", enumValues)));
        }
    }
}
