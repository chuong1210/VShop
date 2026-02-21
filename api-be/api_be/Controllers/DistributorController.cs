
using api_be.Domain.Exceptions;
using api_be.Middleware;
using api_be.Application.Models.Request.DistributorRequest;
using api_be.Application.Responses;
using api_be.Application.Services;
using Microsoft.AspNetCore.Mvc;
using api_be.Domain.ResultResponses;
using api_be.Application.Models.ValidatorRequest.DefaultValidatorBase;

namespace api_be.API.Controllers
{
    [Route("~/smw-api/[controller]")]
    [ApiController]
    public class DistributorController : ControllerBase
    {
        private readonly IDistributorService _distributorService;

        public DistributorController(IDistributorService DistributorService)
        {
            _distributorService = DistributorService;
        }

        /// <summary>
        /// Lấy danh sách nhà cung cấp
        /// </summary>
        /// <remarks>
        /// Ràng buộc: 
        ///
        /// </remarks>
        [HttpGet]
        //[Permission("distributorService.view")]
        public async Task<ActionResult> Get([FromQuery] ListBaseCommand pRequest)
        {
            var response = await _distributorService.GetList(pRequest);

            return StatusCode(response.Code, response);
        }

        /// <summary>
        /// Lấy thông tin NCC theo id
        /// </summary>
        /// <remarks>
        /// Ràng buộc: 
        /// - Id: int, required
        /// </remarks>
        [HttpGet("detail")]
        //[Permission("distributorService.view")]
        public async Task<ActionResult> Get([FromQuery] DetailBaseCommand pRequest)
        {
            var response = await _distributorService.Detail(pRequest);

            return StatusCode(response.Code, response);
        }

        /// <summary>
        /// Thêm NCC mới
        /// </summary>
        /// <remarks>
        /// Ràng buộc:
        /// - InternalCode: string, required, max(50)
        /// - Name: string, required, max(190)
        /// - Phone: string, lenght(10)
        /// - Email: string, email_format
        /// </remarks>
        [HttpPost]
        //[Permission("distributorService.create")]
        public async Task<ActionResult> Post([FromBody] CreateOrUpdateDistributorRequest pRequest)
        {
            var response = await _distributorService.Create(pRequest);

            return StatusCode(response.Code, response);
        }

        /// <summary>
        /// Sửa thông tin NCC
        /// </summary>
        /// <remarks>
        /// Ràng buộc: 
        /// - Id: int, required
        /// </remarks>
        [HttpPut]
        //[Permission("distributorService.update")]
        public async Task<ActionResult> Put([FromBody] CreateOrUpdateDistributorRequest pRequest)
        {
            var response = await _distributorService.Update(pRequest);

            return StatusCode(response.Code, response);
        }

        /// <summary>
        /// Xóa nhà cung cấp theo id
        /// </summary>
        /// <remarks>
        /// Ràng buộc: 
        /// - Id: int, required
        /// </remarks>
        [HttpDelete]
        //[Permission("distributorService.delete")]
        public async Task<ActionResult> Delete([FromQuery] int pRequest)
        {
            try
            {
                await _distributorService.Delete(pRequest);
                return StatusCode(StatusCodes.Status204NoContent);
            }
            catch (NotFoundException ex)
            {
                var responses = Result<DistributorDto>.Failure(ex.Message, StatusCodes.Status404NotFound);
                return StatusCode(responses.Code, responses);
            }
            catch (BadRequestException ex)
            {
                var responses = Result<DistributorDto>.Failure(ex.Message, StatusCodes.Status400BadRequest);
                return StatusCode(responses.Code, responses);
            }
            catch (Exception ex)
            {
                var responses = Result<DistributorDto>.Failure(ex.Message, StatusCodes.Status500InternalServerError);
                return StatusCode(responses.Code, responses);
            }
        }

    }
}
