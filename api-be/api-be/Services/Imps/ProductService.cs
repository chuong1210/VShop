using api_be.Models.Request;
using api_be.Models.Responses;
using api_be.ValidatorRequest.DefaultBase;

namespace api_be.Services.Imps
{
    public class ProductService : IProductService
    {
        public Task<Result<CategoryDto>> Create(CreateCategoryRequest request)
        {
            throw new NotImplementedException();
        }

        public Task<Result<bool>> Delete(int id)
        {
            throw new NotImplementedException();
        }

        public Task<Result<CategoryDto>> Detail(int id)
        {
            throw new NotImplementedException();
        }

        public Task<PaginatedResult<List<CategoryDto>>> GetList(ListBaseCommand request)
        {
            throw new NotImplementedException();
        }

        public Task<Result<CategoryDto>> Update(UpdateCategoryRequest request)
        {
            throw new NotImplementedException();
        }
    }
}
