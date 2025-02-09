using api_be.Application.Models.Request.StaffPossitionRequest;
using api_be.Application.Models.ValidatorRequest.DefaultValidatorBase;
using api_be.Application.Responses;
using api_be.Application.Services;
using api_be.Domain.Exceptions;
using api_be.Domain.ResultResponses;
using api_be.Middleware;

using Microsoft.AspNetCore.Mvc;

namespace api_be.API.Controllers
{
    [Route("~/smw-api/[controller]")]
    [ApiController]
    public class StaffPositionController : ControllerBase
    {
        private readonly IStaffPositionService _staffPositionService;

        public StaffPositionController(IStaffPositionService staffPositionService)
        {
            _staffPositionService = staffPositionService;
        }

        /// <summary>
        /// Lấy danh sách vị trí nhân viên
        /// </summary>
        /// <remarks>
        /// Ràng buộc: 
        ///
        /// </remarks>
        [HttpGet]
        [Permission("staff-position.view")]
        public async Task<ActionResult> Get([FromQuery] ListBaseCommand pRequest)
        {
            var response = await _staffPositionService.GetList(pRequest);

            return StatusCode(response.Code, response);
        }

        /// <summary>
        /// Lấy thông tin vị trí nhân viên theo id
        /// </summary>
        /// <remarks>
        /// Ràng buộc: 
        /// - Id: int, required
        /// </remarks>
        [HttpGet("detail")]
        [Permission("staff-position.view")]
        public async Task<ActionResult> Get([FromQuery] DetailBaseCommand pRequest)
        {
            var response = await _staffPositionService.Detail(pRequest);

            return StatusCode(response.Code, response);
        }

        /// <summary>
        /// Thêm mới vị trí nhân viên
        /// </summary>
        /// <remarks>
        /// Ràng buộc:
        /// - InternalCode: string, required, max(50)
        /// - Name: string, required, max(190)
        /// - Describes: ckeditor
        /// </remarks>
        [HttpPost]
        [Permission("staff-position.create")]
        public async Task<ActionResult> Post([FromBody] CreateOrUpdateStaffPositionRequest pRequest)
        {
            var response = await _staffPositionService.Create(pRequest);

            return StatusCode(response.Code, response);
        }

        /// <summary>
        /// Sửa thông tin vị trí nhân viên
        /// </summary>
        /// <remarks>
        /// Ràng buộc: 
        /// - Id: int, required
        /// </remarks>
        [HttpPut]
        [Permission("staff-position.update")]
        public async Task<ActionResult> Put([FromBody] CreateOrUpdateStaffPositionRequest pRequest)
        {
            var response = await _staffPositionService.Update(pRequest);

            return StatusCode(response.Code, response);
        }

        /// <summary>
        /// Xóa vị trí nhân viên
        /// </summary>
        /// <remarks>
        /// Ràng buộc: 
        /// - Id: int, required
        /// </remarks>
        [HttpDelete]
        [Permission("staff-position.delete")]
        public async Task<ActionResult> Delete([FromQuery] int pRequest)
        {
            try
            {
                await _staffPositionService.Delete(pRequest);
                return StatusCode(StatusCodes.Status204NoContent);
            }
            catch (NotFoundException ex)
            {
                var responses = Result<StaffPositionDto>.Failure(ex.Message, StatusCodes.Status404NotFound);
                return StatusCode(responses.Code, responses);
            }
            catch (BadRequestException ex)
            {
                var responses = Result<StaffPositionDto>.Failure(ex.Message, StatusCodes.Status400BadRequest);
                return StatusCode(responses.Code, responses);
            }
            catch (Exception ex)
            {
                var responses = Result<StaffPositionDto>.Failure(ex.Message, StatusCodes.Status500InternalServerError);
                return StatusCode(responses.Code, responses);
            }
        }

    }
}
