using StackExchange.Redis;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;
using api_be.Middleware;
using Microsoft.Extensions.DependencyInjection;

namespace api_be.Application.Services.Imps
{
    [RegisterService(ServiceLifetime.Scoped)]

    public class RedisInventoryService:IRedisInventoryService
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly ILogger<RedisInventoryService> _logger;
        private readonly string _prefix = "inventory:"; // Prefix để tránh conflict key

        public RedisInventoryService(IConnectionMultiplexer redis, ILogger<RedisInventoryService> logger)
        {
            _redis = redis;
            _logger = logger;
        }

        private IDatabase GetDatabase()
        {
            return _redis.GetDatabase();
        }

        // Lấy số lượng tồn kho của một sản phẩm
        public async Task<int> GetStockLevelAsync(int? productId)
        {
            try
            {
                string key = $"{_prefix}{productId}";
                var value = await GetDatabase().StringGetAsync(key);

                if (value.HasValue && int.TryParse(value, out int stockLevel))
                {
                    return stockLevel;
                }

                // Nếu không tìm thấy, trả về 0 (hoặc giá trị mặc định khác)
                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting stock level from Redis for product {ProductId}", productId);
                // Xử lý lỗi, có thể ném exception hoặc trả về giá trị mặc định
                return -1; // Hoặc một giá trị lỗi khác
            }
        }

        // Cập nhật số lượng tồn kho của một sản phẩm
        public async Task<bool> SetStockLevelAsync(int? productId, int? quantity)
        {
            try
            {
                string key = $"{_prefix}{productId}";
                return await GetDatabase().StringSetAsync(key, quantity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting stock level in Redis for product {ProductId}", productId);
                return false;
            }
        }

        // Tăng số lượng tồn kho của một sản phẩm (ví dụ: khi nhập hàng)
        public async Task<long> IncrementStockLevelAsync(int productId, int quantity)
        {
            try
            {
                string key = $"{_prefix}{productId}";
                return await GetDatabase().StringIncrementAsync(key, quantity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error incrementing stock level in Redis for product {ProductId}", productId);
                return -1;
            }
        }

        // Giảm số lượng tồn kho của một sản phẩm (ví dụ: khi bán hàng)
        public async Task<long> DecrementStockLevelAsync(int? productId, int quantity)
        {
            try
            {
                string key = $"{_prefix}{productId}";
                return await GetDatabase().StringDecrementAsync(key, quantity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error decrementing stock level in Redis for product {ProductId}", productId);
                return -1;
            }
        }

        // Xóa thông tin tồn kho của một sản phẩm (ví dụ: khi xóa sản phẩm)
        public async Task<bool> RemoveStockLevelAsync(int productId)
        {
            try
            {
                string key = $"{_prefix}{productId}";
                return await GetDatabase().KeyDeleteAsync(key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing stock level from Redis for product {ProductId}", productId);
                return false;
            }
        }

    }
}