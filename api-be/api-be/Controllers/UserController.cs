
using api_be.Domain.Interfaces;
using api_be.Middleware;
using api_be.Models;
using api_be.Models.Request;
using api_be.Services;
using api_be.Transforms;
using Azure;
using Azure.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace UI.WebApi.Controllers
{
    [Route("~/smw-api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ICurrentUserService _userService;



        public UserController(IAuthService authService,ICurrentUserService userService)
        {
            _authService = authService;
            _userService = userService;
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
        [HttpGet("users")]
        [Permission("user.view")]
        public async Task<ActionResult> Get([FromQuery] GetListUserRequest pRequest)
        {
            try
            {
                var response = await _authService.GetListUser(pRequest);

                if (!response.Succeeded)
                {
                    return StatusCode(response.Code, response.Messages);
                }

                return StatusCode(response.Code, response);
            }
            catch (Exception ex)
            {
                // Log lỗi chi tiết
                return StatusCode(500, " is Internal server error");
            }
        }

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
        [HttpPost("create")]
        [Permission("user.create")]
        public async Task<ActionResult> Post([FromBody] CreateUserRequest pRequest)
        {
            var response = await _authService.Create(pRequest);

            return StatusCode(response.Code, response);
        }

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
        [HttpPost("assign")]
        [Permission("user.assign")]
        public async Task<ActionResult> AssignRolesForUser([FromBody] AssignRoleUserRequest pRequest)
        {
            var response = await _authService.AssignRole(pRequest);

            return StatusCode(response.Code, response);
        }

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
        [HttpPut("update")]
        //[Permission("user.update")]
        [AllowAnonymous]
        public async Task<ActionResult> Put([FromBody] UpdateUserRequest pRequest)
        {
            var response = await _authService.Update(pRequest);

            return StatusCode(response.Code, response);
        }


        [HttpPost("change-password")]
        [Authorize]
        public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordRequest pRequest)
        {
            var userId = _userService.UserId;
            if (string.IsNullOrEmpty(userId.ToString()))
            {
                // Nếu không có userId (nghĩa là người dùng chưa đăng nhập), trả về lỗi 401 Unauthorized
                return Unauthorized(value: "You must be logged in to change your password." +  IdentityTransform.UnauthorizedException);

            }
            pRequest.UserId = (int)userId;

            // Gọi service để thay đổi mật khẩu
            var response = await _authService.ChangePassword(pRequest);
            return StatusCode(response.Code, response);
        }


        [HttpDelete("{userId}")]
        [Authorize(Roles = "Admin")]  // Chỉ cho phép người dùng có quyền "Admin" mới được phép gọi endpoint này
        public async Task<IActionResult> DeleteUser(int userId)
        {
            // Kiểm tra xem người dùng có quyền admin hay không
            var currentUserId = int.Parse(User.Identity.Name); // Giả sử User.Identity.Name chứa UserId trong token

            // Gọi service để xóa người dùng
            var response = await _authService.Delete(userId, currentUserId);

            return StatusCode(response.Code, response);

        }
    }
}
