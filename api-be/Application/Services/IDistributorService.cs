using api_be.Application.Models.Request;
using api_be.Application.Models.Request.DistributorRequest;
using api_be.Application.Responses;
using api_be.Domain.ResultResponses;
using api_be.Application.Models.ValidatorRequest.DefaultValidatorBase;

namespace api_be.Application.Services
{
    public interface IDistributorService
    {
        public Task<Result<DistributorDto>> Create(CreateOrUpdateDistributorRequest request);
        public Task<Result<DistributorDto>> Update(CreateOrUpdateDistributorRequest request);
        public Task<Result<Boolean>> Delete(int id);

        public Task<Result<DistributorDto>> Detail(DetailBaseCommand request);
        public Task<PaginatedResult<List<DistributorDto>>> GetList(ListBaseCommand request);
    }
}
