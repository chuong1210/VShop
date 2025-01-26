using api_be.Models.Request.DistributorRequest;
using api_be.Models.Request.PaymentRequest;
using api_be.Models.Responses;
using api_be.Models.ValidatorRequest.DefaultBase;
using api_be.ValidatorRequest.DefaultBase;

namespace api_be.Services
{
    public interface IPaymentService
    {
        public Task<Result<PaymentDto>> Create(CreateOrUpdatePaymentRequest request);
        public Task<Result<PaymentDto>> Update(CreateOrUpdatePaymentRequest request);
        public Task<Result<Boolean>> Delete(int id);

        public Task<Result<PaymentDto>> Detail(DetailBaseCommand request);
        public Task<PaginatedResult<List<PaymentDto>>> GetList(ListBaseCommand request);
    }
}
