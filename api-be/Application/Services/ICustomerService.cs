using api_be.Application.Models.Request.CustomerRequest;
using api_be.Application.Responses;
using api_be.Domain.ResultResponses;
using api_be.Application.Models.ValidatorRequest.DefaultValidatorBase;

namespace api_be.Application.Services
{
    public interface ICustomerService
    {
        public Task<Result<CustomerDto>> Update(UpdateCustomerRequest request);
        public Task<Result<Boolean>> Delete(int id);

        public Task<Result<CustomerDto>> Detail(DetailBaseCommand request);
        public Task<PaginatedResult<List<CustomerDto>>> GetList(ListBaseCommand request);
    }
}
