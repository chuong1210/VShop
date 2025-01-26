using api_be.Models.Request;
using api_be.Models.Request.DeliveryRequest;
using api_be.Models.Request.DeliveryRequest;
using api_be.Models.Responses;
using api_be.Models.ValidatorRequest.DefaultBase;
using api_be.ValidatorRequest.DefaultBase;

namespace api_be.Services
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
