using api_be.Domain.Exceptions;

using api_be.Domain.Extensions;
using api_be.Middleware;
using api_be.Domain.Models.Responses;
using api_be.Domain.Transforms;
using CloudinaryDotNet;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Http;
using api_be.Infrastructure.DB;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;

namespace api_be.Application.Services.Imps
{
    [RegisterService(ServiceLifetime.Scoped)]

    public class ImageService: IImageSerivce
    {
        private readonly SupermarketDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly Cloudinary _cloudinary;

        public ImageService(SupermarketDbContext context, IHttpClientFactory httpClientFactory, Cloudinary cloudinary)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _cloudinary = cloudinary;
        }
        public async Task<Result<bool>> uploadImage(IFormFile file)
        {

            var result = await CloudinaryExtension.UploadImageAsync(file, _cloudinary);
            return Result<bool>.Success(true, StatusCodes.Status200OK);


        }

        public async Task<FileContentResult> GetProductImageAsync(int productId, int? index = null)
        {
            var product = await _context.Products
                .AsNoTracking()
                .Where(p => p.Id == productId)
                .Select(p => new { p.Images })
                .FirstOrDefaultAsync();

            if (product == null)
                throw new NotFoundException(ValidatorTransform.NotExistsValueInTable(Modules.Id, productId.ToString()));

            // Convert comma-separated string to a list
            var imageUrls = product.Images?.Split(',').ToList();

            if (imageUrls == null || !imageUrls.Any())
                 throw new NotFoundException(ValidatorTransform.MustUrls(Modules.Product.Images));


            // Get specific image or default to the first
            var imageUrl = imageUrls.ElementAtOrDefault(index ?? 0);
            if (imageUrl == null)
                throw new ArgumentOutOfRangeException(ValidatorTransform.ValidValue(Modules.Product.Images));

            var httpClient = _httpClientFactory.CreateClient();
            var response = await httpClient.GetAsync(imageUrl);

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException(IdentityTransform.FailtToFetch(imageUrl));

            var imageData = await response.Content.ReadAsByteArrayAsync();
            return new FileContentResult(imageData, "image/jpeg");
        }
    }
}
