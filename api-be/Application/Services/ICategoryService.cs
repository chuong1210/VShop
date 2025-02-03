using api_be.Domain.Models.Request;
using api_be.Domain.Models.Responses;
using api_be.Domain.Models;
using api_be.Domain.DefaultValidatorBase;
using api_be.Domain.DefaultValidatorBase;

namespace api_be.Application.Services
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
