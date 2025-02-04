using api_be.Core.Entities;
using api_be.Domain.Models.Request;
using api_be.Domain.Models.Responses;
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
        Task<Result<string>> MarkMessageAsReadAsync(int messageId);
    }
}
