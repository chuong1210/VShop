using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace api_be.Application.Services
{
    public interface  IRedisInventoryService
    {
        Task<int> GetStockLevelAsync(int? productId);
        Task<bool> SetStockLevelAsync(int? productId, int? quantity);
        Task<long> IncrementStockLevelAsync(int productId, int quantity);
        Task<long> DecrementStockLevelAsync(int? productId, int quantity);
        Task<bool> RemoveStockLevelAsync(int productId);
    }
}
