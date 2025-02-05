using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;

namespace api_be.Application.Services.HubService
{
    public class ChatHubService : Hub
    {
        public override Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier;
            if (!string.IsNullOrEmpty(userId))
            {
                Groups.AddToGroupAsync(Context.ConnectionId, userId);
            }
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            return base.OnDisconnectedAsync(exception);
        }

        // 📌 Sự kiện khi người dùng bắt đầu nhập tin nhắn
        public async Task UserTyping(int receiverId)
        {
            var senderId = Context.UserIdentifier; // Lấy ID của người gửi
            if (senderId != null)
            {
                await Clients.Group(receiverId.ToString())
                    .SendAsync("UserTyping", senderId);
            }
        }

        // 📌 Sự kiện khi người dùng dừng nhập tin nhắn
        public async Task UserStoppedTyping(int receiverId)
        {
            var senderId = Context.UserIdentifier;
            if (senderId != null)
            {
                await Clients.Group(receiverId.ToString())
                    .SendAsync("UserStoppedTyping", senderId);
            }
        }
    }
}
