using api_be.Middleware;
using Microsoft.Extensions.Caching.Memory;

namespace api_be.Services.Imps
{
    [RegisterService(ServiceLifetime.Scoped)]
    public class MemoryTokenService : IRedisTokenService
    {
        private readonly IMemoryCache _cache;
        public MemoryTokenService(IMemoryCache cache)
        {
            _cache = cache;
        }

        public async Task AddInvalidatedToken(string jwtId, DateTime expiryTime)
        {
            var expiry = expiryTime - DateTime.UtcNow;

            _cache.Set($"invalidated_token:{jwtId}", true, expiry);
            await Task.CompletedTask;
        }

        public async Task CacheRefreshToken(string userId, string refreshToken, DateTime expiryTime)
        {
            var expiry = expiryTime - DateTime.UtcNow;

            // Đảm bảo expiry không âm
            if (expiry <= TimeSpan.Zero)
            {
                expiry = TimeSpan.FromMinutes(1); // Thời gian tồn tại tối thiểu
            }

            _cache.Set($"refresh_token:{userId}", refreshToken, expiry);
            await Task.CompletedTask;
        }

        public async Task<string> GetCachedRefreshToken(string userId)
        {
            _cache.TryGetValue($"refresh_token:{userId}", out string refreshToken);
            return await Task.FromResult(refreshToken);
        }

        public async Task<bool> IsTokenInvalidated(string jwtId)
        {
            return await Task.FromResult(_cache.TryGetValue($"invalidated_token:{jwtId}", out _));
        }

        public async Task RemoveCachedRefreshToken(string userId)
        {
            _cache.Remove($"refresh_token:{userId}");
            await Task.CompletedTask;
        }
    }
}
