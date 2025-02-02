using api_be.Core.Domain;

namespace api_be.Core.Entities
{
    public class Message: AuditableEntity
    {
        public int SenderId { get; set; } // ID của người gửi
        public int ReceiverId { get; set; } // ID của người nhận
        public string Content { get; set; } // Nội dung tin nhắn
        public DateTime SentAt { get; set; } // Thời gian gửi
        public bool IsRead { get; set; } // Đã đọc hay chưa
    }
}
