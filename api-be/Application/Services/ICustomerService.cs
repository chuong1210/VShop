using api_be.Domain.Models.Request.CustomerRequest;
using api_be.Domain.Models.Responses;
using api_be.Domain.DefaultValidatorBase;
using api_be.Domain.DefaultValidatorBase;

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
