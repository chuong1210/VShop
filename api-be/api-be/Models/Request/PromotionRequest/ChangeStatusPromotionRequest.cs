using static api_be.Entities.Promotion;

namespace api_be.Models.Request.PromotionRequest
{
    public record ChangeStatusPromotionRequest
    {
        public int? PromotionId { get; set; }

        public PromotionStatus? Status { get; set; }
    }
}
