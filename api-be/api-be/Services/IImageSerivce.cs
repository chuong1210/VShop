using api_be.Models.Responses;
using Microsoft.AspNetCore.Mvc;

namespace api_be.Services
{
    public interface IImageSerivce
    {
        public Task<Result<bool>> uploadImage(IFormFile file);
        Task<FileContentResult> GetProductImageAsync(int productId, int? index = null);

    }
}
