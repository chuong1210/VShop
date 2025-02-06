using api_be.Core.Domain.Interfaces;
using static api_be.Core.Entities.Promotion;
using api_be.Application.Models.ValidatorRequest.DefaultValidatorBase;

namespace api_be.Application.Models.Request.PromotionRequest
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
