using api_be.Models.Responses;
using api_be.ValidatorRequest.DefaultBase;

namespace api_be.Services
{
    public interface IProductElasticsearchService
    {
        Task<PaginatedResult<List<ProductDto>>> GetList(ListBaseCommand request);
        Task ReindexProducts();
    }
}
