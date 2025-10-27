using api_be.Application.Models.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace api_be.Application.Responses
{
    public record MessageDto:BaseDto
    {
        public new string Id { get; set; }   // Ẩn Id int của BaseDto, dùng string riêng cho Message

        public int SenderId { get; set; }     // ID của người gửi
        public int ReceiverId { get; set; }   // ID của người nhận
        public string Content { get; set; }   // Nội dung tin nhắn
        public DateTime SentAt { get; set; }  // Thời gian gửi
        public bool IsRead { get; set; }      // Đã đọc hay chưa
    }
}
