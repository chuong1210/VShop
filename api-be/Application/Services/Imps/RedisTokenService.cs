


using api_be.Middleware;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace api_be.Application.Services.Imps
{
    [RegisterService(ServiceLifetime.Scoped)]

    public class RedisTokenService : IRedisTokenService
    {
        private readonly IDatabase _redisDb;
        public RedisTokenService(IConnectionMultiplexer redisConnection)
        {
            _redisDb = redisConnection.GetDatabase();
        }

        public async Task AddInvalidatedToken(string jwtId, DateTime expiryTime)
        {
            var expiry = expiryTime - DateTime.UtcNow;
            await _redisDb.StringSetAsync(
                $"invalidated_token:{jwtId}",
                "1",
                expiry
            );
        }

        public async Task CacheRefreshToken(string userId, string refreshToken , DateTime expiryTime)
        {
            TimeSpan expiry = expiryTime - DateTime.UtcNow;

            // Ensure the expiry is not negative
            if (expiry <= TimeSpan.Zero)
            {
                expiry = TimeSpan.FromMinutes(1); // Minimum expiry time
            }

            await _redisDb.StringSetAsync(
                $"refresh_token:{userId}",
                refreshToken,
                expiry
            );
        }

        public async Task<string> GetCachedRefreshToken(string userId)
        {
            return await _redisDb.StringGetAsync($"refresh_token:{userId}");
        }

        public async Task<bool> IsTokenInvalidated(string jwtId)
        {
            return await _redisDb.KeyExistsAsync($"invalidated_token:{jwtId}");
        }

        public async Task RemoveCachedRefreshToken(string userId)
        {
            await _redisDb.KeyDeleteAsync($"refresh_token:{userId}");
        }
    }
}
