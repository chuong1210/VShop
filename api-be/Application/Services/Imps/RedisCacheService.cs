using api_be.Application.Models.ValidatorRequest.DefaultValidatorBase;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using StackExchange.Redis;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
namespace api_be.Application.Services.Imps
{


public class RedisCacheService : IRedisCacheService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisCacheService> _logger;

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
            if (value.HasValue)
            {
                return JsonConvert.DeserializeObject<T>(value);
            }
            return default;
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
            var serializedValue = JsonConvert.SerializeObject(value);
            await GetDatabase().StringSetAsync(key, serializedValue, expiry);
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
            await GetDatabase().KeyDeleteAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing data from Redis for key {Key}", key);
        }
    }
}
}
