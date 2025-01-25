using api_be.Models.Request;
using api_be.Models.Request.Distributor;
using api_be.Models.Responses;
using api_be.Models.ValidatorRequest.DefaultBase;
using api_be.ValidatorRequest.DefaultBase;

namespace api_be.Services
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
