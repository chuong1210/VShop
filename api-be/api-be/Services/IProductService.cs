using api_be.Models.Request;
using api_be.Models.Responses;
using api_be.Models.ValidatorRequest.DefaultBase;
using api_be.ValidatorRequest.DefaultBase;
using Microsoft.AspNetCore.Mvc;

namespace api_be.Services
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
