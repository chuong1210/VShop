using api_be.Core.Domain;

namespace api_be.Core.Entities
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
