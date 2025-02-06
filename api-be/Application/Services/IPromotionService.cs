using api_be.Application.Models.Request;
using api_be.Application.Models.Request.PromotionRequest;
using api_be.Application.Responses;
using api_be.Domain.ResultResponses;
using api_be.Application.Models.ValidatorRequest.DefaultValidatorBase;

namespace api_be.Application.Services
{
    public interface IPromotionService
    {
        public Task<Result<PromotionDto>> Create(CreateOrUpdatePromotionRequest request);
        public Task<Result<PromotionDto>> Update(CreateOrUpdatePromotionRequest request);
        public Task<Result<Boolean>> ApplyPromotionForProduct(ApplyPromotionForProductRequest request);

        public Task<Result<Boolean>> Delete(int id);
        public Task<Result<PromotionDto>> ChangeStatus(ChangeStatusPromotionRequest request);

        public Task<Result<PromotionDto>> Detail(DetailBaseCommand request);
        public Task<PaginatedResult<List<PromotionDto>>> GetList(ListBaseCommand request);
    }
}
