using api_be.Application.Models.ValidatorRequest.DefaultValidatorBase;
using api_be.Middleware;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Threading.Tasks;

namespace api_be.Application.Services.Imps
{
    [RegisterService(ServiceLifetime.Scoped)]
    public class RedisCacheService : IRedisCacheService
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly ILogger<RedisCacheService> _logger;

        // Configure JSON serializer settings
        private readonly JsonSerializerSettings _jsonSettings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Auto,
            NullValueHandling = NullValueHandling.Ignore,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
            // Bỏ qua lỗi nếu không tìm thấy constructor
            Error = (sender, args) =>
            {
                args.ErrorContext.Handled = true;
            }
        };

        public RedisCacheService(IConnectionMultiplexer redis, ILogger<RedisCacheService> logger)
        {
            _redis = redis;
            _logger = logger;
        }

        private IDatabase GetDatabase()
        {
            return _redis.GetDatabase();
        }

        public async Task<T?> GetAsync<T>(string key)
        {
            try
            {
                var value = await GetDatabase().StringGetAsync(key);

                if (!value.HasValue)
                {
                    _logger.LogDebug("Cache miss for key: {Key}", key);
                    return default;
                }

                try
                {
                    var result = JsonConvert.DeserializeObject<T>(value, _jsonSettings);
                    _logger.LogDebug("Cache hit for key: {Key}", key);
                    return result;
                }
                catch (JsonException jsonEx)
                {
                    _logger.LogWarning(jsonEx,
                        "Failed to deserialize cached value for key {Key}. Removing invalid cache entry.",
                        key);

                    // Xóa cache entry bị lỗi
                    await RemoveAsync(key);
                    return default;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting data from Redis for key {Key}", key);
                return default;
            }
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan expiry)
        {
            try
            {
                var serializedValue = JsonConvert.SerializeObject(value, _jsonSettings);
                await GetDatabase().StringSetAsync(key, serializedValue, expiry);
                _logger.LogDebug("Successfully cached data for key: {Key}", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting data in Redis for key {Key}", key);
            }
        }

        public async Task RemoveAsync(string key)
        {
            try
            {
                var deleted = await GetDatabase().KeyDeleteAsync(key);
                if (deleted)
                {
                    _logger.LogDebug("Successfully removed cache for key: {Key}", key);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing data from Redis for key {Key}", key);
            }
        }

        // Thêm method để clear cache theo pattern
        public async Task RemoveByPatternAsync(string pattern)
        {
            try
            {
                var server = _redis.GetServer(_redis.GetEndPoints()[0]);
                var keys = server.Keys(pattern: pattern);

                var tasks = new List<Task>();
                foreach (var key in keys)
                {
                    tasks.Add(RemoveAsync(key));
                }

                await Task.WhenAll(tasks);
                _logger.LogInformation("Successfully removed cache entries matching pattern: {Pattern}", pattern);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing cache by pattern: {Pattern}", pattern);
            }
        }
    }
}