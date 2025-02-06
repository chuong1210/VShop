using api_be.Application.Models.Request.ImportGoodRequest;
using api_be.Application.Models.Request.SupplierOrderRequest;
using api_be.Application.Models.ValidatorRequest.DefaultValidatorBase;
using api_be.Application.Models.ValidatorRequest.SupllierOrderValidator;
using api_be.Application.Responses;
using api_be.Domain.ResultResponses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace api_be.Application.Services
{
    public interface ISupplierOrderService
    {
        public Task<Result<SupplierOrderDto>> Create(CreateOrUpdateSupplierOrderRequest request);
        public Task<Result<SupplierOrderDto>> Update(CreateOrUpdateSupplierOrderRequest request);

        public Task<Result<bool>> Delete(int id);
        public Task<Result<bool>> ChangeStatus(ChangeStatusSupplierOrderRequest request);

        public Task<Result<SupplierOrderDto>> Detail(DetailBaseCommand request);
        public Task<PaginatedResult<List<ImportGoodDto>>> GetList(ListBaseCommand request);
    }
}
