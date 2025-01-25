using api_be.Models.Request.CustomerRequest;
using api_be.Models.Responses;
using api_be.Models.ValidatorRequest.DefaultBase;
using api_be.ValidatorRequest.DefaultBase;

namespace api_be.Services
{
    public interface ICustomerService
    {
        public Task<Result<CustomerDto>> Update(UpdateCustomerRequest request);
        public Task<Result<Boolean>> Delete(int id);

        public Task<Result<CustomerDto>> Detail(DetailBaseCommand request);
        public Task<PaginatedResult<List<CustomerDto>>> GetList(ListBaseCommand request);
    }
}
