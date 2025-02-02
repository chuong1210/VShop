using api_be.Domain.Models.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace api_be.Application.Services
{
    public interface IImageSerivce
    {
        public Task<Result<bool>> uploadImage(IFormFile file);
        Task<FileContentResult> GetProductImageAsync(int productId, int? index = null);

    }
}
