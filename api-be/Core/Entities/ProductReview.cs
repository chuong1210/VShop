using api_be.Core.Domain;
using api_be.Core.Domain.Interfaces;
using api_be.Core.Entities.Auth;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Sieve.Attributes;

namespace api_be.Core.Entities
{
    public class ProductReview : IAuditableEntity

    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public int Id { get; set; }

        [Sieve(CanFilter = true, CanSort = true)]
        public int ProductId { get; set; }
        public virtual Product Product { get; set; }

        [Sieve(CanFilter = true, CanSort = true)]
        public int UserId { get; set; }
        public virtual User User { get; set; }

        [Sieve(CanFilter = true, CanSort = true)]
        public int Rating { get; set; } // 1 - 5 sao

        public string? ReviewText { get; set; }
        [Sieve(CanFilter = true, CanSort = true)]
        public int? ParentCommentId { get; set; } // Cho phép bình luận con (reply)
        public virtual ProductReview? ParentComment { get; set; }


        [Sieve(CanFilter = true, CanSort = true)]
        public bool IsApproved { get; set; } = false; // Quản trị viên có thể duyệt

        // Danh sách ảnh/video đính kèm
        //public virtual ICollection<ProductReviewMedia> Media { get; set; } = new List<ProductReviewMedia>();
        [BsonElement("createdAt")]
        public DateTime? CreatedAt { get; set; }

        [BsonElement("createdBy")]
        public string? CreatedBy { get; set; }

        [BsonElement("updatedAt")]
        public DateTime? UpdatedAt { get; set; }

        [BsonElement("updatedBy")]
        public string? UpdatedBy { get; set; }

        [BsonElement("isDeleted")]
        public bool? IsDeleted { get; set; }

    }
}
