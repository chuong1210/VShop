
using api_be.Middleware;
using api_be.Models;
using api_be.Models.Request;
using api_be.Services;
using Azure;
using Azure.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace UI.WebApi.Controllers
{
    [Route("~/smw-api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IAuthService _authService;


        public UserController(IAuthService authService)
        {
            _authService = authService;
        }   


        /// <summary>
        /// Đăng nhập người dùng
        /// </summary>
        /// <remarks>
        /// Ràng buộc: 
        /// - UserName: string 3->50
        /// - Password: string 6->50
        /// </remarks>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult> Login([FromBody] LoginAccountRequest pRequest)
        {
            try
            {
                var response = await _authService.Login(pRequest);
                //return Ok(new { Token = token });
                return StatusCode(response.Code, response);

            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized("Invalid credentials");
            }

        }




        ///// <summary>
        ///// Lấy danh người dùng
        ///// </summary>
        ///// <remarks>
        ///// Ràng buộc: 
        /////
        ///// </remarks>
        //[HttpGet]
        //[Permission("user.view")]
        //public async Task<ActionResult> Get([FromQuery] UserDto pRequest)
        //{
        //    var response = await _mediator.Send(pRequest);

        //    return StatusCode(response.Code, response);
        //}

        ///// <summary>
        ///// Tạo người dùng mới
        ///// </summary>
        ///// <remarks>
        ///// Ràng buộc: 
        ///// - UserName: string 3->50
        ///// - Password: string 6->50
        ///// - PhoneNumber: required
        ///// - Email: required
        ///// </remarks>
        //[HttpPost]
        //[Permission("user.create")]
        //public async Task<ActionResult> Post([FromBody] UserDto pRequest)
        //{
        //    var response = await _mediator.Send(pRequest);

        //    return StatusCode(response.Code, response);
        //}

        /// <summary>
        /// Đăng ký tài khoản người dùng mới
        /// </summary>
        /// <remarks>
        /// Ràng buộc: 
        /// - UserName: string 3->50
        /// - Password: string 6->50
        /// - PhoneNumber: required
        /// - Email: required
        /// </remarks>
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<ActionResult> Register([FromBody] RegisterAccountRequest request)
        {
            var response = await _authService.Register(request);

            if (!response.Succeeded)
                return StatusCode(response.Code, response);
             return StatusCode(response.Code, response);

            //return CreatedAtAction(nameof(Register), new { id = response.Data.Id }, response.Data);
        }

        ///// <summary>
        ///// Gán vai trò cho người dùng
        ///// </summary>
        ///// <remarks>
        ///// Ràng buộc: 
        ///// - UserId: int
        ///// - RolesId: array int
        ///// </remarks>
        //[HttpPost("assign")]
        //[Permission("user.assign")]
        //public async Task<ActionResult> AssignRolesForUser([FromBody] UserDto pRequest)
        //{
        //    var response = await _mediator.Send(pRequest);

        //    return StatusCode(response.Code, response);
        //}

        ///// <summary>
        ///// Link người dùng tới tài khoản user supper admin
        ///// </summary>
        ///// <remarks>
        ///// Ràng buộc: 
        ///// - UserId: int
        ///// - RolesId: array int
        ///// </remarks>
        //[HttpPatch("link-staff")]
        //[Permission("user-staff.link")]
        //public async Task<ActionResult> LinkUserToStaff([FromBody] UserDto pRequest)
        //{

        //}

        ///// <summary>
        ///// Sửa thông tin người dùng
        ///// </summary>
        ///// <remarks>
        ///// Ràng buộc: 
        ///// - PhoneNumber: required
        ///// - Email: required
        ///// </remarks>
        //[HttpPut]
        //[Permission("user.update")]
        //public async Task<ActionResult> Put([FromBody] UserDto pRequest)
        //{

        //}
    }
}
