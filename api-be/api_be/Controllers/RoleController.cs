

using api_be.Domain.Exceptions;
using api_be.Middleware;
using api_be.Application.Models.Request.RoleRequest;
using api_be.Application.Responses;
using api_be.Application.Services;
using Microsoft.AspNetCore.Mvc;
using api_be.Domain.ResultResponses;
using api_be.Application.Models.ValidatorRequest.DefaultValidatorBase;

namespace api_be.API.Controllers
{
    [Route("~/smw-api/[controller]")]
    [ApiController]

    public class RoleController : ControllerBase
    {
        private readonly IRoleService _roleService;

        public RoleController(IRoleService mediator)
        {
            _roleService = mediator;
        }

        /// <summary>
        /// Lấy danh vai trò
        /// </summary>
        /// <remarks>
        /// Ràng buộc: 
        ///
        /// </remarks>
        [HttpGet]
        //[Permission("role.view")]
        public async Task<ActionResult> Get([FromQuery] ListBaseCommand pRequest)
        {
            var response = await _roleService.GetList(pRequest);

            return StatusCode(response.Code, response);
        }

        /// <summary>
        /// Lấy danh vai trò theo controller
        /// </summary>
        /// <remarks>
        /// Ràng buộc: 
        ///
        /// </remarks>
        [HttpGet("list-with-controller")]
        //[Permission("role.view")]
        public async Task<ActionResult> GetByController([FromQuery] ListBaseCommand pRequest)
        {
            var response = await _roleService.GetListRoleWithPermission(pRequest);

            return StatusCode(response.Code, response);
        }

        /// <summary>
        /// Lấy thông tin vai trò theo id
        /// </summary>
        /// <remarks>
        /// Ràng buộc: 
        /// - Id: int, required
        /// </remarks>
        [HttpGet("detail")]
        //[Permission("role.view")]
        public async Task<ActionResult> Get([FromQuery] DetailBaseCommand pRequest)
        {
            var response = await _roleService.Detail(pRequest);

            return StatusCode(response.Code, response);
        }

        /// <summary>
        /// Thêm vai trò mới
        /// </summary>
        /// <remarks>
        /// Ràng buộc:
        /// - Name: string, required, max(190)
        /// </remarks>
        [HttpPost]
        //[Permission("role.create")]
        public async Task<ActionResult> Post([FromBody] CreateOrUpdateRoleRequest pRequest)
        {
            var response = await _roleService.Create(pRequest);

            return StatusCode(response.Code, response);
        }

        /// <summary>
        /// Sửa tên vai trò
        /// </summary>
        /// <remarks>
        /// Ràng buộc: 
        /// - Id: int, required
        /// - Name: string, required, max(190)
        /// </remarks>
        [HttpPut]
        //[Permission("role.update")]
        public async Task<ActionResult> Put([FromBody] CreateOrUpdateRoleRequest pRequest)
        {
            var response = await _roleService.Update(pRequest);

            return StatusCode(response.Code, response);
        }

        /// <summary>
        /// Gán quyền cho vai trò
        /// </summary>
        /// <remarks>
        /// Ràng buộc: 
        /// - RoleId: int
        /// - PermessionsName: array string
        /// </remarks>
        [HttpPost("assign")]
        //[Permission("role.assign")]
        public async Task<ActionResult> AssignPermissionForRole([FromBody] AssignPermissionsForRoleRequest pRequest)
        {
            var response = await _roleService.AssignPermissionsForRole(pRequest);

            return StatusCode(response.Code, response);
        }




        // 2. Delete Role
        //[HttpDelete("{id}")]
        [HttpDelete]

        public async Task<IActionResult> Delete(int id)
        {
            try

            {
                var result = await _roleService.Delete(id);

                return StatusCode(StatusCodes.Status204NoContent, result.Data); // Return success
            }
            catch (NotFoundException ex)
            {
                var responses = Result<CategoryDto>.Failure(ex.Message, StatusCodes.Status404NotFound);
                return StatusCode(responses.Code, responses);
            }
            catch (BadRequestException ex)
            {
                var responses = Result<CategoryDto>.Failure(ex.Message, StatusCodes.Status400BadRequest);
                return StatusCode(responses.Code, responses);
            }
            catch (Exception ex)
            {
                var responses = Result<CategoryDto>.Failure(ex.Message, StatusCodes.Status500InternalServerError);
                return StatusCode(responses.Code, responses);
            }
        }
    }
}
