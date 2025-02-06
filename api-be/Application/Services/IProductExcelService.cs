using api_be.Application.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using api_be.Domain.ResultResponses;
using api_be.Application.Models.ValidatorRequest.DefaultValidatorBase;

namespace api_be.Application.Services
{
    public interface IProductExcelService
    {
        Task<Result<List<ProductDto>>> ImportProductsFromExcel(IFormFile  file);
        Task<Stream> ExportProductsToExcel(ListBaseCommand request);

    }
}
