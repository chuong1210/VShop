using api_be.Application.Responses;
using api_be.Application.Models.Request;
using api_be.Core.Entities;
using api_be.Domain.ResultResponses;
using api_be.Application.Models.ValidatorRequest.DefaultValidatorBase;

namespace api_be.Application.Services
{
    public interface IProductElasticsearchService
    {
        Task<PaginatedResult<List<ProductDto>>> GetListSearchProduct(ListBaseCommand request);
        //Task<List<ProductDto>> SearchDetailedProductsAsync(string searchTerm);
        //Task<IQueryable<Product>> GetListProductAsync();

    }
}
