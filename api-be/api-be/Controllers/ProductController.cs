using api_be.Exceptions;
using api_be.Extensions;
using api_be.Middleware;
using api_be.Models.Request;
using api_be.Models.Responses;
using api_be.Models.ValidatorRequest.DefaultBase;
using api_be.Services;
using api_be.Services.Imps;
using api_be.Transforms;
using api_be.ValidatorRequest.DefaultBase;
using CloudinaryDotNet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace UI.WebApi.Controllers
{
    [Route("~/smw-api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class ProductController : ControllerBase
    {
        private readonly IProductService _productService;
        private readonly IImageSerivce _imageSerivce;
        private readonly Cloudinary _cloudinary;


        public ProductController(IProductService productService,Cloudinary cloudinary, IImageSerivce imageSerivce)
        {
            _productService = productService;
            _cloudinary = cloudinary;
            _imageSerivce = imageSerivce;
        }

        /// <summary>
        /// Lấy danh sách sản phẩm
        /// </summary>
        /// <remarks>
        /// Ràng buộc: 
        ///
        /// </remarks>
        [HttpGet]
        //[Permission("product.view")]
        public async Task<ActionResult> Get([FromQuery] ListBaseCommand pRequest)
        {
            var response = await _productService.GetList(pRequest);

            return StatusCode(response.Code, response);
        }

        /// <summary>
        /// Lấy danh sách sản phẩm theo nhóm khuyến mãi
        /// </summary>
        /// <remarks>
        /// Ràng buộc: 
        ///
        /// </remarks>
        [HttpGet("combo-products")]
        //[Permission("product.view")]
        public async Task<ActionResult> GetCombo([FromQuery] ListBaseCommand pRequest)
        {
            var response = await _productService.GetListPromotionComBo(pRequest);

            return StatusCode(response.Code, response);
        }

        /// <summary>
        /// Lấy thông tin sản phẩm theo id
        /// </summary>
        /// <remarks>
        /// Ràng buộc: 
        /// - Id: int, required
        /// </remarks>
        [HttpGet("detail")]
        //[Permission("product.view")]
        public async Task<ActionResult> Get([FromQuery] DetailBaseCommand pRequest)
        {
            var response = await _productService.Detail(pRequest);

            return StatusCode(response.Code, response);
        }

        /// <summary>
        /// Thêm sản phẩm mới
        /// </summary>
        /// <remarks>
        /// Ràng buộc:
        /// - InternalCode: string, required, max(50)
        /// - Name: string, required, max(190)
        /// - Images: array url
        /// - Price: > 0
        /// - Describes: ckeditor
        /// - Feature: ckeditor
        /// - Specifications: ckeditor
        /// - CategoryId: id có trong Category
        /// </remarks>
        [HttpPost]
        //[Permission("product.create")]
        public async Task<ActionResult> Post([FromBody] CreateProductRequest pRequest)
        {
            var response = await _productService.Create(pRequest);

            return StatusCode(response.Code, response);
        }

        /// <summary>
        /// Sửa thông tin sản phẩm
        /// </summary>
        /// <remarks>
        /// Ràng buộc: 
        /// - Id: int, required
        /// </remarks>
        [HttpPut]
        //[Permission("product.update")]
        public async Task<ActionResult> Put([FromBody] UpdateProductRequest pRequest)
        {
            var response = await _productService.Update(pRequest);

            return StatusCode(response.Code, response);
        }

        /// <summary>
        /// Nhận viên thay đổi trạng thái sản phẩm
        /// </summary>
        /// <remarks>
        /// Ràng buộc:
        /// - OrderId: Id của sản phẩm
        /// - Status: Trạng thái thay đổi của đơn hàng
        /// Draft(0), Active(1), Pause(2), OutStock(3), Stop(4)
        /// </remarks>
        [HttpPatch]
        //[Permission("product.change-status")]
        public async Task<ActionResult> Change([FromBody] ChangeStatusProductRequest pRequest)
        {
            var response = await _productService.ChangeStatus(pRequest);

            return StatusCode(response.Code, response);
        }


        ///// <summary>
        ///// Xóa sản phẩm 
        ///// </summary>
        ///// <remarks>
        ///// Ràng buộc: 
        ///// - Id: int, required
        ///// </remarks>
        [HttpDelete]
        //[Permission("product.delete")]
        public async Task<ActionResult> Delete([FromQuery] int id)
        {
            try
            {
                await _productService.Delete(id);
                return StatusCode(StatusCodes.Status204NoContent);
            }
            catch (NotFoundException ex)
            {
                var responses = Result<ProductDto>.Failure(ex.Message, StatusCodes.Status404NotFound);
                return StatusCode(responses.Code, responses);
            }
            catch (BadRequestException ex)
            {
                var responses = Result<ProductDto>.Failure(ex.Message, StatusCodes.Status400BadRequest);
                return StatusCode(responses.Code, responses);
            }
            catch (Exception ex)
            {
                var responses = Result<ProductDto>.Failure(ex.Message, StatusCodes.Status500InternalServerError);
                return StatusCode(responses.Code, responses);
            }
        }



        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile([FromForm] IFormFile file)
        {
           
                var responses = await _imageSerivce.uploadImage(file);
                return StatusCode(responses.Code, responses);

      
        }

        [HttpGet("{id}/images")]
        public async Task<IActionResult> GetProductImage(int id, [FromQuery] int? index = null)
        {
            try
            {
                var result = await _imageSerivce.GetProductImageAsync(id, index);
                return result;
            }
            catch (KeyNotFoundException ex)
            {
                var responses = Result<ProductDto>.Failure(ex.Message, StatusCodes.Status404NotFound);
                return StatusCode(responses.Code, responses);

            }
            catch (ArgumentOutOfRangeException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(500, new { Message = ex.Message });
            }
        }
    }
}
