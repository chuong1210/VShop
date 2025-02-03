using static api_be.Core.Entities.Coupon;

namespace api_be.Domain.Models.Request.CouponRequest
{
    public class ChangeStatusCouponRequest
    {
        public int? CouponId { get; set; }

        public CouponStatus? Status { get; set; }
    }
}
