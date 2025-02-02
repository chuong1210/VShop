namespace api_be.Application.Services
{
    public interface ICacheService
    {
        Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> getDataFunc, TimeSpan? expiration = null);
        Task RemoveAsync(string key);
        Task RemoveByPrefixAsync(string prefix);
    }
}
