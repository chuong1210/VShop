using api_be.Domain.Models.Request;
using api_be.Domain.Models.Request.CouponRequest;
using api_be.Domain.Models.Responses;
using api_be.Domain.DefaultValidatorBase;

namespace api_be.Application.Services
{
    public interface ICouponService
    {
        public Task<Result<CouponDto>> Create(CreateOrUpdateCopuponRequest request);
        public Task<Result<CouponDto>> Update(CreateOrUpdateCopuponRequest request);
        public Task<Result<Boolean>> Delete(int id);
        public Task<Result<CouponDto>> ChangeStatus(ChangeStatusCouponRequest request);

        public Task<Result<CouponDto>> Detail(DetailBaseCommand request);
        public Task<PaginatedResult<List<CouponDto>>> GetList(ListBaseCommand request);
    }
}
