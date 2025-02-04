using api_be.Core.Domain;
using api_be.Core.Domain.Interfaces;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace api_be.Core.Entities
{
    public class Message : IAuditableEntity
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("senderId")]
        public int SenderId { get; set; }

        [BsonElement("receiverId")]
        public int ReceiverId { get; set; }

        [BsonElement("content")]
        public string Content { get; set; }

        [BsonElement("sentAt")]
        public DateTime SentAt { get; set; }

        [BsonElement("isRead")]
        public bool IsRead { get; set; } = false;

        // Các trường cho auditable entity
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
