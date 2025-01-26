using api_be.Models.Responses;
using api_be.ValidatorRequest.DefaultBase;
using Microsoft.AspNetCore.Mvc;

namespace api_be.Services
{
    public interface IProductExcelService
    {
        Task<Result<List<ProductDto>>> ImportProductsFromExcel(IFormFile file);
        Task<Stream> ExportProductsToExcel(ListBaseCommand request);

    }
}
