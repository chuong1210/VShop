using api_be.Core.Domain.Interfaces;
using static api_be.Core.Entities.Coupon;
using api_be.Application.Models.ValidatorRequest.DefaultValidatorBase;

namespace api_be.Application.Models.Request.CouponRequest
{
    public record CreateOrUpdateCopuponRequest:UpdateBaseCommand , IBaseCoupon
    {

        public string? InternalCode { get; set; }

        public string? Name { get; set; }

        public DateTime? Start { get; set; }

        public DateTime? End { get; set; }

        public int? Limit { get; set; }

        // Giảm giá
        public int? Discount { get; set; }

        public int? PercentMax { get; set; }

        // Giảm %
        public int? Percent { get; set; }

        public int? DiscountMax { get; set; }

        public CouponType? Type { get; set; }

        public CType TypeC { get; set; }

        // Khách hàng: Nếu là MC
        public int? CustomerId { get; set; }

        // Áp dụng cho hoá đơn: Nếu SC: Thực hiện sau khi có hoá đơn
    }
}
