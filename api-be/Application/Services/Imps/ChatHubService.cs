using api_be.Core.Domain.Interfaces;
using api_be.Core.Entities;
using api_be.Domain.Extensions;
using System;

using System.Threading.Tasks;
using api_be.Infrastructure.DB;
using Microsoft.AspNetCore.SignalR;

namespace api_be.Application.Services.Imps
{

    public class ChatHubService : Hub
    {
        private readonly ISupermarketDbContext _context;

        public ChatHubService(ISupermarketDbContext context)
        {
            _context = context;
        }

        public async Task SendMessage(int senderId, int receiverId, string content)
        {
            // Lưu tin nhắn vào cơ sở dữ liệu
            var message = new Message
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                Content = content,
                SentAt = DateTime.UtcNow,
                IsRead = false
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            // Gửi tin nhắn thời gian thực tới người nhận
            await Clients.User(receiverId.ToString()).SendAsync("ReceiveMessage", senderId, content, message.SentAt);
        }

        public override Task OnConnectedAsync()
        {
            // Gán ConnectionId cho user
            var userId = Context.UserIdentifier;
            if (!string.IsNullOrEmpty(userId))
            {
                Groups.AddToGroupAsync(Context.ConnectionId, userId);
            }
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            // Xử lý khi user ngắt kết nối
            return base.OnDisconnectedAsync(exception);
        }
    }

}
