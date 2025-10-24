using api_be.Core.Domain;
using api_be.Core.Domain.Interfaces;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using Sieve.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace api_be.Core.Entities
{
    public class ProductReviewMedia:IHardDeleteEntity
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public int Id { get; set; }
        [Sieve(CanFilter = true, CanSort = true)]
        public int CommentId { get; set; }
        public virtual ProductReview Comment { get; set; }

        public string MediaUrl { get; set; } // Đường dẫn ảnh/video

        public MediaType Type { get; set; } // Ảnh hoặc video

        public enum MediaType
        {
            Image,
            Video
        }
    }
}
