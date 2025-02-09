using api_be.Application.Models.Request.CouponRequest;
using api_be.Application.Models.Request.StaffPossitionRequest;
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
    public interface IStaffPositionService
    {
        public Task<Result<StaffPositionDto>> Create(CreateOrUpdateStaffPositionRequest request);

        public Task<Result<StaffPositionDto>> Update(CreateOrUpdateStaffPositionRequest request);
        public Task<Result<Boolean>> Delete(int id);

        public Task<Result<StaffPositionDto>> Detail(DetailBaseCommand request);
        public Task<PaginatedResult<List<StaffPositionDto>>> GetList(ListBaseCommand request);
    }
}
