using api_be.Application.Models.Request.PromotionRequest;
using api_be.Application.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using api_be.Domain.ResultResponses;
using api_be.Application.Models.ValidatorRequest.DefaultValidatorBase;
using api_be.Application.Models.Request.ImportGoodRequest;

namespace api_be.Application.Services
{
    public interface IImportGoodsService
    {
        public Task<Result<ImportGoodDto>> Create(CreateImportGoodsRequest request);
        public Task<Result<ImportGoodDto>> Update(UpdateImportGoodsRequest request);

        public Task<Result<bool>> Delete(int id);
        public Task<Result<bool>> ChangeStatus(ChangeStatusImportGoodsRequest request);

        public Task<Result<ImportGoodDto>> Detail(DetailBaseCommand request);
        public Task<PaginatedResult<List<ImportGoodDto>>> GetList(ListBaseCommand request);
    }
}
