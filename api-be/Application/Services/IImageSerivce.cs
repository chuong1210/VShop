
using api_be.Application.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using api_be.Domain.ResultResponses;


namespace api_be.Application.Services
{
    public interface IImageSerivce
    {
        public Task<Result<bool>> uploadImage(IFormFile file);
        Task<FileContentResult> GetProductImageAsync(int productId, int? index = null);

    }
}
