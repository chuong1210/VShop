using api_be.Application.Models.ValidatorRequest.DefaultValidatorBase;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace api_be.Application.Services.Imps
{
    public class RedisCacheService:ICacheService
    {
        private readonly IDistributedCache _cache;
        private readonly ILogger<RedisCacheService> _logger;

        public RedisCacheService(IDistributedCache cache, ILogger<RedisCacheService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> getDataFunc, TimeSpan? expiration = null)
        {
            var cachedData = await _cache.GetAsync(key);
            if (cachedData != null)
            {
                try
                {
                    return JsonSerializer.Deserialize<T>(cachedData);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deserializing cached data for key: {Key}", key);
                    await _cache.RemoveAsync(key);
                }
            }

            var data = await getDataFunc();
            if (data != null)
            {
                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromMinutes(30)
                };

                try
                {
                    var serializedData = JsonSerializer.SerializeToUtf8Bytes(data);
                    await _cache.SetAsync(key, serializedData, options);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error caching data for key: {Key}", key);
                }
            }

            return data;
        }

        public async Task RemoveAsync(string key)
        {
            await _cache.RemoveAsync(key);
        }

        public async Task RemoveByPrefixAsync(string prefix)
        {
            // Note: This is a basic implementation. For Redis, you might want to use scan command
            // to find and delete keys by pattern
            throw new NotImplementedException("Implement based on your cache provider");
        }
    }

    // Constants/CacheKeys.cs
    public static class CacheKeys
    {
        public const string ProductPrefix = "product_";
        public const string ProductList = ProductPrefix + "list_";
        public const string ProductDetail = ProductPrefix + "detail_";

        public static string GetProductListKey(ListBaseCommand request)
        {
            return $"{ProductList}{request.Page}_{request.PageSize}_{request.Filters}_{request.Sorts}";
        }

        public static string GetProductDetailKey(int productId)
        {
            return $"{ProductDetail}{productId}";
        }
    }
}
