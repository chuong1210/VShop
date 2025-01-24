namespace api_be.Services
{
    public interface IRedisTokenService
    {

        Task CacheRefreshToken(string userId, string refreshToken, DateTime expiryTime);
            Task<string> GetCachedRefreshToken(string userId);
            Task RemoveCachedRefreshToken(string userId);
            Task AddInvalidatedToken(string jwtId, DateTime expiryTime);
            Task<bool> IsTokenInvalidated(string jwtId);
    }
}
