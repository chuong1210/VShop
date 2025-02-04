using api_be.Core.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace api_be.Domain.Models.Request
{
    public record MessageRequest:IBaseMessage
    {
        public int? SenderId { get; set; }
        public int? ReceiverId { get; set; }
        public string? Content { get; set; }
        public DateTime ?SentAt { get; set; }
        public bool? IsRead { get; set; }
     
    }
}
