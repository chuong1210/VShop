using api_be.Models.Request;
using api_be.Models.Request.PromotionRequest;
using api_be.Models.Responses;
using api_be.Models.ValidatorRequest.DefaultBase;
using api_be.ValidatorRequest.DefaultBase;

namespace api_be.Services
{
    public interface IPromotionService
    {
        public Task<Result<PromotionDto>> Create(CreateOrUpdatePromotionRequest request);
        public Task<Result<PromotionDto>> Update(CreateOrUpdatePromotionRequest request);
        public Task<Result<Boolean>> ApplyPromotionForProduct(ApplyPromotionForProductRequest request);

        public Task<Result<Boolean>> Delete(int id);
        public Task<Result<Boolean>> ChangeStatus(ChangeStatusPromotionRequest request);

        public Task<Result<PromotionDto>> Detail(DetailBaseCommand request);
        public Task<PaginatedResult<List<PromotionDto>>> GetList(ListBaseCommand request);
    }
}
