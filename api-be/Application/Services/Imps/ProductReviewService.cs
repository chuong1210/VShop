using api_be.Application.Services.HubService;
using api_be.Core.Domain.Interfaces;
using api_be.Core.Entities;
using api_be.Infrastructure.Data;
using api_be.Infrastructure.DB.Interceptors;
using AutoMapper;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace api_be.Application.Services.Imps
{
   public class ProductReviewService
    {
        private readonly IMongoCollection<ProductReview> _reviews;
        private readonly MongoDbInterceptor _mongoDbInterceptor;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;

        public ProductReviewService(MongoDbContext context, IMapper mapper, ICurrentUserService currentUserService,MongoDbInterceptor mongoDbInterceptor)
        {
            _reviews = context.ProductReviews;
            _mapper = mapper;
            _currentUserService = currentUserService;
            _mongoDbInterceptor = mongoDbInterceptor;
        }

        public async Task<List<ProductReview>> GetProductReviewsAsync(int productId)
        {
            return await _reviews.Find(r => r.ProductId == productId && r.IsApproved).ToListAsync();
        }

        public async Task<ProductReview> CreateReviewAsync(ProductReview review)
        {
            await _reviews.InsertOneAsync(review);
            return review;
        }

        public async Task<bool> UpdateReviewApprovalAsync(int reviewId, bool isApproved)
        {
            var update = Builders<ProductReview>.Update
                .Set(r => r.IsApproved, isApproved)
                .Set(r => r.UpdatedAt, DateTime.UtcNow);

            var result = await _reviews.UpdateOneAsync(
                r => r.Id == reviewId,
                update);

            return result.ModifiedCount > 0;
        }

        public async Task<List<ProductReview>> GetPendingReviewsAsync()
        {
            return await _reviews.Find(r => !r.IsApproved).ToListAsync();
        }
    }
}
