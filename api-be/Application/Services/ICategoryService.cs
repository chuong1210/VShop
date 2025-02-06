using api_be.Application.Models.Request;
using api_be.Application.Responses;
using api_be.Domain.ResultResponses;
using api_be.Application.Models.ValidatorRequest.DefaultValidatorBase;

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
