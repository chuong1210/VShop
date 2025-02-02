using api_be.Domain.Models.Request.DistributorRequest;
using api_be.Domain.Models.Request.PaymentRequest;
using api_be.Domain.Models.Responses;
using api_be.Domain.DefaultValidatorBase;
using api_be.Domain.DefaultValidatorBase;

namespace api_be.Application.Services
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
