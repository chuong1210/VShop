using api_be.Domain.Interfaces;
using api_be.Models.ValidatorRequest.DefaultBase;
using static api_be.Entities.Promotion;

namespace api_be.Models.Request.PromotionRequest
{
    public record CreateOrUpdatePromotionRequest:UpdateBaseCommand, IBasePromotion
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

        public PromotionType? Type { get; set; }

    }
}
