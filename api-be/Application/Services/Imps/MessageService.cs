using api_be.Application.Services.HubService;
using api_be.Application.ValidatorRequest;
using api_be.Core.Domain.Interfaces;
using api_be.Core.Entities;
using api_be.Domain.Models.Request;
using api_be.Domain.Models.Responses;
using api_be.Infrastructure.Data;
using api_be.Infrastructure.DB.Interceptors;
using api_be.Middleware;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace api_be.Application.Services.Imps
{
    [RegisterService(ServiceLifetime.Scoped)]

    public class MessageService:IMessageService
    {
        private readonly IMongoCollection<Message> _messages;

        private readonly MongoDbInterceptor _mongoDbInterceptor;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;
        private readonly IHubContext<ChatHubService> _chatHubContext;

        public MessageService(MongoDbContext context, IMapper mapper, ICurrentUserService currentUserService, IHubContext<ChatHubService> chatHubContext)
        {
            _messages = context.Messages;
            _mapper = mapper;
            _currentUserService = currentUserService;
            _chatHubContext = chatHubContext;
        }

        public async Task<Result<MessageDto>> InsertMessageAsync(MessageRequest request)
        {
            try
            {
                var validator = new MessageValidator();
                var validationResult = await validator.ValidateAsync(request);

                if (validationResult.IsValid == false)
                {
                    var errorMessages = validationResult.Errors.Select(x => x.ErrorMessage).ToList();
                    return Result<MessageDto>.Failure(errorMessages, StatusCodes.Status400BadRequest);
                }
                var message = _mapper.Map<Message>(request);


                message.Id = ObjectId.GenerateNewId().ToString();
                _mongoDbInterceptor.BeforeInsert(message);
                message.SenderId = (int)_currentUserService.UserId;
                message.SentAt = DateTime.UtcNow;
                await _messages.InsertOneAsync(message);
                var messageDto = _mapper.Map<MessageDto>(message);

                // Gửi tin nhắn qua SignalR
                await _chatHubContext.Clients.Group(message.ReceiverId.ToString())
                    .SendAsync("ReceiveMessage", messageDto);

                return Result<MessageDto>.Success(messageDto, StatusCodes.Status201Created);

            }
            catch (Exception ex)
            {
                return Result<MessageDto>.Failure($"An error occurred: {ex.Message}", StatusCodes.Status500InternalServerError);
            }
        }



        public async Task<Result<List<MessageDto>>> GetConversationAsync(int userId, int correspondentId)
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

            var messages = _messages.Find(filter).SortBy(m => m.SentAt).ToListAsync();

            var messageDtos = _mapper.Map<List<MessageDto>>(messages);
            return Result<List<MessageDto>>.Success(messageDtos, StatusCodes.Status201Created);

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
