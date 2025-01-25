using static api_be.Entities.Coupon;

namespace api_be.Models.Request.CouponRequest
{
    public class ChangeStatusCouponRequest
    {
        public int? CouponId { get; set; }

        public CouponStatus? Status { get; set; }
    }
}
