using api_be.Domain.Models.Request;
using api_be.Domain.Models.Request.DeliveryRequest;
using api_be.Domain.Models.Request.DeliveryRequest;
using api_be.Domain.Models.Responses;
using api_be.Domain.DefaultValidatorBase;
using api_be.Domain.DefaultValidatorBase;

namespace api_be.Application.Services
{
    public interface IDeliveryService
    {
        public Task<Result<DeliveryDto>> Create(CreateOrUpdateDeliveryRequest request);
        public Task<Result<DeliveryDto>> Update(CreateOrUpdateDeliveryRequest request);
        public Task<Result<Boolean>> Delete(int id);
        public Task<Result<Boolean>> ChangeStatus(ChangeStatusDeliveryRequest request);


        public Task<Result<DeliveryDto>> Detail(DetailBaseCommand request);
        public Task<PaginatedResult<List<DeliveryDto>>> GetList(ListBaseCommand request);
    }
}
