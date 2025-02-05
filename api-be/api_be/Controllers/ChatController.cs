using api_be.Core.Domain.Interfaces;
using api_be.Domain.Models.Request;
using api_be.Domain.Models.Responses;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Collections.Generic;
using api_be.Application.Services;
using Microsoft.AspNetCore.Authorization;

namespace api_be.API.Controllers
{
    [ApiController]
    [Route("~/smw-api/[controller]")]
    [AllowAnonymous]
    public class ChatController : ControllerBase
    {
        private readonly IMessageService _chatService;

        public ChatController(IMessageService chatService)
        {
            _chatService = chatService;
        }

        /// <summary>
        /// Gửi tin nhắn
        /// </summary>
        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromBody] MessageRequest request)
        {
            var result = await _chatService.InsertMessageAsync(request);
            return StatusCode(result.Code, result);

        }

        /// <summary>
        /// Lấy lịch sử cuộc trò chuyện giữa 2 người dùng
        /// </summary>
        [HttpGet("conversation")]
        public async Task<IActionResult> GetConversation([FromQuery] int userId, [FromQuery] int correspondentId)
        {
            var result = await _chatService.GetConversationAsync(userId, correspondentId);
            return StatusCode(result.Code, result);

        }

        /// <summary>
        /// Đánh dấu tin nhắn đã đọc
        /// </summary>
        [HttpPost("mark-read")]
        public async Task<IActionResult> MarkMessageAsRead([FromBody] int messageId)
        {
            var result = await _chatService.MarkMessageAsReadAsync(messageId);
            if (!result.Succeeded) return StatusCode(result.Code, result);

            return Ok(result);
        }
    }
}
