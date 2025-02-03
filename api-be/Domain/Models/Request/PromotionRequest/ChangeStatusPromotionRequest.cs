using static api_be.Core.Entities.Promotion;

namespace api_be.Domain.Models.Request.PromotionRequest
{
    public record ChangeStatusPromotionRequest
    {
        public int? PromotionId { get; set; }

        public PromotionStatus? Status { get; set; }
    }
}
