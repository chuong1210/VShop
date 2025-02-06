using api_be.Application.Models.Request.DistributorRequest;
using api_be.Application.Models.Request.PaymentRequest;
using api_be.Application.Responses;
using api_be.Domain.ResultResponses;
using api_be.Application.Models.ValidatorRequest.DefaultValidatorBase;

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
