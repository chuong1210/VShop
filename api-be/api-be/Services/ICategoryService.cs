using api_be.Models.Request;
using api_be.Models.Responses;
using api_be.Models;
using api_be.ValidatorRequest.DefaultBase;
using api_be.Models.ValidatorRequest.DefaultBase;

namespace api_be.Services
{
    public interface ICategoryService
    {
        public Task<Result<CategoryDto>> Create(CreateCategoryRequest request);
        public Task<Result<CategoryDto>> Update(UpdateCategoryRequest request);
        public Task<Result<Boolean>> Delete(int id);
        public Task<Result<CategoryDto>> Detail(DetailBaseCommand request);
        public Task<PaginatedResult<List<CategoryDto>>> GetList(ListBaseCommand request);

    }
}
