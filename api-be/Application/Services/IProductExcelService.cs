using api_be.Domain.Models.Responses;
using api_be.Domain.DefaultValidatorBase;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace api_be.Application.Services
{
    public interface IProductExcelService
    {
        Task<Result<List<ProductDto>>> ImportProductsFromExcel(IFormFile  file);
        Task<Stream> ExportProductsToExcel(ListBaseCommand request);

    }
}
