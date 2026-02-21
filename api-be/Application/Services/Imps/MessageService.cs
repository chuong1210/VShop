using api_be.Application.Models.Request;
using api_be.Application.Models.ValidatorRequest;
using api_be.Application.Responses;
using api_be.Application.Services.HubService;
using api_be.Core.Domain.Interfaces;
using api_be.Core.Entities;
using api_be.Domain.ResultResponses;
using api_be.Infrastructure.Data;
using api_be.Infrastructure.DB;
using api_be.Infrastructure.DB.Interceptors;
using api_be.Middleware;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace api_be.Application.Services.Imps
{
    [RegisterService(ServiceLifetime.Scoped)]
    public class MessageService : IMessageService
    {
        private readonly IMongoCollection<Message> _messages;
        private readonly MongoDbInterceptor _mongoDbInterceptor;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;
        private readonly IHubContext<ChatHubService> _chatHubContext;
        private readonly ISupermarketDbContext _context; // Thêm DbContext để lấy User info

        public MessageService(
            MongoDbContext mongoContext,
            IMapper mapper,
            ICurrentUserService currentUserService,
            IHubContext<ChatHubService> chatHubContext,
            MongoDbInterceptor mongoDbInterceptor,
            ISupermarketDbContext context) // Inject DbContext
        {
            _messages = mongoContext.Messages;
            _mapper = mapper;
            _currentUserService = currentUserService;
            _chatHubContext = chatHubContext;
            _mongoDbInterceptor = mongoDbInterceptor;
            _context = context;
        }
        public async Task<Result<MessageDto>> InsertMessageAsync(MessageRequest request)
        {
            try
            {
                var validator = new MessageValidator();
                var validationResult = await validator.ValidateAsync(request);

                if (!validationResult.IsValid)
                {
                    var errorMessages = validationResult.Errors.Select(x => x.ErrorMessage).ToList();
                    return Result<MessageDto>.Failure(errorMessages, StatusCodes.Status400BadRequest);
                }

                if (_currentUserService.UserId == null)
                {
                    return Result<MessageDto>.Failure("User is not authenticated", StatusCodes.Status401Unauthorized);
                }

                var message = _mapper.Map<Message>(request);
                message.Id = ObjectId.GenerateNewId().ToString();
                message.SenderId = _currentUserService.UserId.Value;
                message.SentAt = DateTime.UtcNow;
                message.IsRead = false;

                _mongoDbInterceptor.BeforeInsert(message);

                await _messages.InsertOneAsync(message);

                // ✅ Lấy thông tin sender từ database
                var sender = await _context.Users.FirstOrDefaultAsync(u => u.Id == message.SenderId);

                // ✅ Lấy thông tin receiver để có userName khi cần
                var receiver = await _context.Users.FirstOrDefaultAsync(u => u.Id == message.ReceiverId);

                // ✅ Create enriched message with complete info
                var enrichedMessage = new
                {
                    id = message.Id,
                    senderId = message.SenderId,
                    receiverId = message.ReceiverId,
                    content = message.Content,
                    sentAt = message.SentAt,
                    isRead = message.IsRead,
                    senderName = sender?.UserName ?? $"User {message.SenderId}",
                    senderAvatar = "https://www.pngmart.com/files/23/Free-Logos-PNG-Clipart.png",
                    receiverName = receiver?.UserName ?? $"User {message.ReceiverId}" // ✅ Add this for conversation list
                };

                Console.WriteLine($"📤 Sending message via SignalR:");
                Console.WriteLine($"   Sender: {message.SenderId} -> Receiver: {message.ReceiverId}");
                Console.WriteLine($"   Content: {message.Content}");

                // ✅ Send to RECEIVER
                await _chatHubContext.Clients.Group(message.ReceiverId.ToString())
                    .SendAsync("ReceiveMessage", enrichedMessage);
                Console.WriteLine($"   ✅ Sent to receiver group: {message.ReceiverId}");

                // ✅ Send to SENDER
                await _chatHubContext.Clients.Group(message.SenderId.ToString())
                    .SendAsync("ReceiveMessage", enrichedMessage);
                Console.WriteLine($"   ✅ Sent to sender group: {message.SenderId}");

                var messageDto = _mapper.Map<MessageDto>(message);
                return Result<MessageDto>.Success(messageDto, StatusCodes.Status201Created);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error sending message: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return Result<MessageDto>.Failure($"An error occurred: {ex.Message}", StatusCodes.Status500InternalServerError);
            }
        }
        public async Task<Result<List<MessageDto>>> GetConversationAsync(int userId, int correspondentId)
        {
            try
            {
                var filter = Builders<Message>.Filter.Or(
                    Builders<Message>.Filter.And(
                        Builders<Message>.Filter.Eq(m => m.SenderId, userId),
                        Builders<Message>.Filter.Eq(m => m.ReceiverId, correspondentId)
                    ),
                    Builders<Message>.Filter.And(
                        Builders<Message>.Filter.Eq(m => m.SenderId, correspondentId),
                        Builders<Message>.Filter.Eq(m => m.ReceiverId, userId)
                    )
                );

                var messages = await _messages.Find(filter)
                    .SortBy(m => m.SentAt)
                    .ToListAsync();

                var messageDtos = _mapper.Map<List<MessageDto>>(messages);

                // ✅ Enrich với thông tin sender
                var senderIds = messages.Select(m => m.SenderId).Distinct().ToList();
                var users = await _context.Users
                    .Where(u => senderIds.Contains(u.Id))
                    .ToDictionaryAsync(u => u.Id, u => u);

                foreach (var dto in messageDtos)
                {
                    if (users.TryGetValue(dto.SenderId, out var sender))
                    {
                        dto.SenderName = sender.UserName ?? $"User {dto.SenderId}";
                        dto.SenderAvatar = "https://www.pngmart.com/files/23/Free-Logos-PNG-Clipart.png";
                    }
                }

                return Result<List<MessageDto>>.Success(messageDtos, StatusCodes.Status200OK);
            }
            catch (Exception ex)
            {
                return Result<List<MessageDto>>.Failure($"An error occurred: {ex.Message}", StatusCodes.Status500InternalServerError);
            }
        }

        public async Task<Result<string>> MarkMessageAsReadAsync(string messageId)
        {
            try
            {
                var update = Builders<Message>.Update.Set(m => m.IsRead, true);
                var result = await _messages.UpdateOneAsync(
                    Builders<Message>.Filter.Eq(m => m.Id, messageId),
                    update
                );

                if (result.ModifiedCount == 0)
                {
                    return Result<string>.Failure("Message not found or already read", StatusCodes.Status404NotFound);
                }

                return Result<string>.Success("Message marked as read", StatusCodes.Status200OK);
            }
            catch (Exception ex)
            {
                return Result<string>.Failure($"An error occurred: {ex.Message}", StatusCodes.Status500InternalServerError);
            }
        }

        public async Task<Result<List<ConversationDto>>> GetConversations()
        {
            try
            {
                var currentUserId = _currentUserService.UserId;

                if (currentUserId == null)
                {
                    return Result<List<ConversationDto>>.Failure("User is not authenticated", StatusCodes.Status401Unauthorized);
                }

                // Lấy tất cả tin nhắn liên quan đến user hiện tại
                var filter = Builders<Message>.Filter.Or(
                    Builders<Message>.Filter.Eq(m => m.SenderId, currentUserId.Value),
                    Builders<Message>.Filter.Eq(m => m.ReceiverId, currentUserId.Value)
                );

                var allMessages = await _messages.Find(filter).ToListAsync();

                // Lấy danh sách user đã chat
                var conversationUserIds = allMessages
                    .Select(m => m.SenderId == currentUserId.Value ? m.ReceiverId : m.SenderId)
                    .Distinct()
                    .ToList();

                var conversations = new List<ConversationDto>();

                foreach (var userId in conversationUserIds)
                {
                    // Lấy tin nhắn cuối cùng
                    var conversationMessages = allMessages
                        .Where(m => (m.SenderId == currentUserId.Value && m.ReceiverId == userId) ||
                                   (m.SenderId == userId && m.ReceiverId == currentUserId.Value))
                        .OrderByDescending(m => m.SentAt)
                        .ToList();

                    var lastMessage = conversationMessages.FirstOrDefault();

                    if (lastMessage == null) continue;

                    // Đếm tin nhắn chưa đọc
                    var unreadCount = conversationMessages
                        .Count(m => m.SenderId == userId &&
                                   m.ReceiverId == currentUserId.Value &&
                                   !m.IsRead);

                    // Lấy thông tin user từ SQL Server
                    var user = await _context.Users
                        .FirstOrDefaultAsync(u => u.Id == userId);

                    if (user != null)
                    {
                        conversations.Add(new ConversationDto
                        {
                            UserId = userId,
                            UserName = user.UserName ?? user.UserName ?? $"User {userId}",
                            UserAvatar = "https://www.pngmart.com/files/23/Free-Logos-PNG-Clipart.png",
                            LastMessage = lastMessage.Content ?? "",
                            LastMessageTime = lastMessage.SentAt,
                            UnreadCount = unreadCount
                        });
                    }
                }

                // Sắp xếp theo thời gian tin nhắn cuối
                conversations = conversations
                    .OrderByDescending(c => c.LastMessageTime)
                    .ToList();

                return Result<List<ConversationDto>>.Success(conversations, StatusCodes.Status200OK);
            }
            catch (Exception ex)
            {
                return Result<List<ConversationDto>>.Failure($"An error occurred: {ex.Message}", StatusCodes.Status500InternalServerError);
            }
        }

        public async Task<Result<string>> MarkMessageAsReadAsync(int messageId)
        {
            var update = Builders<Message>.Update.Set(m => m.IsRead, true);
            var result = await _messages.UpdateOneAsync(Builders<Message>.Filter.Eq(m => m.Id, messageId.ToString()), update);

            if (result.ModifiedCount == 0)
                return Result<string>.Failure("Message not found or already read", StatusCodes.Status404NotFound);

            return Result<string>.Success("Message marked as read", StatusCodes.Status200OK);
        }
    }
}