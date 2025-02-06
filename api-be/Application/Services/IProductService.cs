using api_be.Application.Models.Request;
using api_be.Application.Responses;
using Microsoft.AspNetCore.Mvc;
using api_be.Domain.ResultResponses;
using api_be.Application.Models.ValidatorRequest.DefaultValidatorBase;

namespace api_be.Application.Services
{
    public interface IProductService
    {
        public Task<Result<ProductDto>> Create(CreateProductRequest request);
        public Task<Result<ProductDto>> Update(UpdateProductRequest request);
        public Task<Result<Boolean>> Delete(int id);
        public Task<Result<Boolean>> ChangeStatus(ChangeStatusProductRequest request);

        public Task<Result<ProductDto>> Detail(DetailBaseCommand request );
        public Task<PaginatedResult<List<ProductDto>>> GetList(ListBaseCommand request);
        public Task<PaginatedResult<List<PromotionComboProductDto>>> GetListPromotionComBo(ListBaseCommand request);
      


    }
}
