using Microsoft.EntityFrameworkCore;
using Sieve.Models;
using Sieve.Services;

namespace api_be.ValidatorRequest.ListBase
{
    public static class PaginatedResultBase
    {
        public async static Task<int> CountApplySieveAsync<T>(ISieveProcessor pIsieveProcessor, SieveModel pSieve, IQueryable<T> pQuery)
        {
            SieveModel sieveModel = new SieveModel();
            sieveModel.Filters = pSieve.Filters;
            sieveModel.Sorts = pSieve.Sorts;
            var queryCount = pIsieveProcessor.Apply(sieveModel, pQuery);
            int totalCount = await queryCount.CountAsync();

            return totalCount;
        }
    }
}
