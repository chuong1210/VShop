using api_be.Core.Domain.Interfaces;
using api_be.Core.Entities;
using api_be.Domain.Extensions;
using api_be.Domain.Models.Request;
using api_be.Domain.Models.Responses;
using api_be.Domain.ValidatorRequest.BaseProduct;
using api_be.Domain.DefaultValidatorBase;
using ClosedXML.Excel;
using System.Data;

using api_be.Infrastructure.DB;
using Microsoft.Extensions.Logging;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace api_be.Application.Services.Imps
{
    public class ProductExcelService:IProductExcelService
    {
        private readonly ISupermarketDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<ProductExcelService> _logger;

        public ProductExcelService(
            ISupermarketDbContext context,
            IMapper mapper,
            ILogger<ProductExcelService> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }
        private DataTable ConvertWorksheetToDataTable(IXLWorksheet worksheet)
        {
            DataTable dataTable = new DataTable();

            // Add columns
            foreach (var firstRowCell in worksheet.FirstRow().Cells())
            {
                dataTable.Columns.Add(firstRowCell.Value.ToString());
            }

            // Add rows
            foreach (var row in worksheet.RowsUsed().Skip(1))
            {
                var dataRow = dataTable.NewRow();
                for (int i = 0; i < dataTable.Columns.Count; i++)
                {
                    dataRow[i] = row.Cell(i + 1).Value.ToString();
                }
                dataTable.Rows.Add(dataRow);
            }

            return dataTable;
        }
        public async Task<Result<List<ProductDto>>> ImportProductsFromExcel(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return Result<List<ProductDto>>.Failure("File is empty", StatusCodes.Status400BadRequest);

                using var stream = new MemoryStream();
                await file.CopyToAsync(stream);

                using var workbook = new XLWorkbook(stream);
                var worksheet = workbook.Worksheet(1);
                var dataTable = ConvertWorksheetToDataTable(worksheet);

                var productsToImport = new List<Product>();
                var errors = new List<string>();

                for (int row = 2; row <= worksheet.RowsUsed().Count(); row++)
                {
                    var productRequest = MapExcelRowToProductRequest(worksheet, row);
                    var validator = new BaseProductValidator(_context);
                    var validationResult = await validator.ValidateAsync(productRequest);

                    if (!validationResult.IsValid)
                    {
                        errors.AddRange(validationResult.Errors.Select(e =>
                            $"Row {row}: {e.ErrorMessage}"));
                        continue;
                    }

                    var product = _mapper.Map<Product>(productRequest);
                    product.Type = Core.Entities.Product.ProductType.Option;
                    product.Status = Core.Entities.Product.ProductStatus.Draft;
                    product.Quantity = 0;

                    productsToImport.Add(product);
                }

                if (errors.Any())
                    return Result<List<ProductDto>>.Failure(errors, StatusCodes.Status400BadRequest);

                await _context.Products.AddRangeAsync(productsToImport);
                await _context.SaveChangesAsync();

                var productDtos = _mapper.Map<List<ProductDto>>(productsToImport);
                return Result<List<ProductDto>>.Success(productDtos, StatusCodes.Status200OK);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing products from Excel");
                return Result<List<ProductDto>>.Failure(ex.Message, StatusCodes.Status500InternalServerError);
            }
        }

        private CreateProductRequest MapExcelRowToProductRequest(IXLWorksheet worksheet, int row)
        {
            return new CreateProductRequest
            {
                InternalCode = worksheet.Cell(row, 1).Value.ToString(),
                Name = worksheet.Cell(row, 2).Value.ToString(),
                CategoryId = worksheet.Cell(row, 3).GetValue<int?>(),
                Price = worksheet.Cell(row, 4).GetValue<decimal>(),
                Describes = worksheet.Cell(row, 5).Value.ToString(),
                Feature = worksheet.Cell(row, 6).Value.ToString(),
                Specifications = worksheet.Cell(row, 7).Value.ToString(),
                Images = worksheet.Cell(row, 8).Value.ToString().Split(',').ToList()
            };
        }

        public async Task<Stream> ExportProductsToExcel(ListBaseCommand request)
        {
            try
            {
                var products = await _context.Set<Product>()
                    .FilterDeleted()
                    .Where(x => x.Type == Core.Entities.Product.ProductType.Option)
                    .ToListAsync();

                var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Products");

                // Add headers
                worksheet.Cell(1, 1).Value = "Internal Code";
                worksheet.Cell(1, 2).Value = "Name";
                worksheet.Cell(1, 3).Value = "Category ID";
                worksheet.Cell(1, 4).Value = "Price";
                worksheet.Cell(1, 5).Value = "Description";
                worksheet.Cell(1, 6).Value = "Feature";
                worksheet.Cell(1, 7).Value = "Specifications";
                worksheet.Cell(1, 8).Value = "Images";

                // Add product data
                for (int i = 0; i < products.Count; i++)
                {
                    var product = products[i];
                    worksheet.Cell(i + 2, 1).Value = product.InternalCode;
                    worksheet.Cell(i + 2, 2).Value = product.Name;
                    worksheet.Cell(i + 2, 3).Value = product.CategoryId;
                    worksheet.Cell(i + 2, 4).Value = product.Price;
                    worksheet.Cell(i + 2, 5).Value = product.Describes;
                    worksheet.Cell(i + 2, 6).Value = product.Feature;
                    worksheet.Cell(i + 2, 7).Value = product.Specifications;
                    worksheet.Cell(i + 2, 8).Value = product.Images;
                }

                var stream = new MemoryStream();
                workbook.SaveAs(stream);
                stream.Position = 0;
                return stream;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting products to Excel");
                throw;
            }
        }
    }
}
