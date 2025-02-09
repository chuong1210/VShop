using api_be.Application.Models.Request.CouponRequest;
using api_be.Application.Models.Request.StaffRequest;
using api_be.Application.Models.ValidatorRequest.DefaultValidatorBase;
using api_be.Application.Responses;
using api_be.Domain.ResultResponses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace api_be.Application.Services
{
    public interface IStaffService
    {
        public Task<Result<StaffDto>> Create(CreateOrUpdateStaffRequest request);

        public Task<Result<StaffDto>> Update(CreateOrUpdateStaffRequest request);
        public Task<Result<Boolean>> Delete(int id);

        public Task<Result<StaffDto>> Detail(DetailBaseCommand request);
        public Task<PaginatedResult<List<StaffDto>>> GetList(ListBaseCommand request);
    }
}
