using api_be.Application.Models.Request;
using api_be.Application.Responses;
using api_be.Core.Entities;
using api_be.Domain.ResultResponses;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace api_be.Application.Services
{
    public interface IMessageService
    {
        Task<Result<MessageDto>> InsertMessageAsync(MessageRequest message);
        Task<Result<List<MessageDto>>> GetConversationAsync(int userId, int correspondentId);
        Task<Result<string>> MarkMessageAsReadAsync(string messageId);
        Task<Result<List<ConversationDto>>> GetConversations();

    }
}
