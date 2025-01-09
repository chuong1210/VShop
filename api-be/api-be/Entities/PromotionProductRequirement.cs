using api_be.Domain.Common;

namespace api_be.Domain.Entities
{
    public class PromotionProductRequirement : HardDeleteEntity
    {
        // -1: Single
        // Ngược lại gop cụm và khuyến mãi nếu có đủ
        public int? Group { get; set; }

        public int? PromotionId { get; set; }

        public Promotion? Promotion { get; set; }

        public int? ProductId { get; set; }

        public Product? Product { get; set; }
    }
}
