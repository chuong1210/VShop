using api_be.Middleware;
using api_be.Domain.Models.Request.CustomerRequest;
using api_be.Domain.DefaultValidatorBase;
using api_be.Application.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace api_be.Controllers
{
    [Route("~/smw-api/[controller]")]
    [ApiController]
    public class CustomerController : ControllerBase
    {
        private readonly ICustomerService _customerService;

        public CustomerController(ICustomerService CustomerService)
        {
            _customerService = CustomerService;
        }

        /// <summary>
        /// Lấy danh sách khách hàng
        /// </summary>
        /// <remarks>
        /// Ràng buộc: 
        ///
        /// </remarks>
        [HttpGet]
        [Permission("customer.view")]
        public async Task<ActionResult> Get([FromQuery] ListBaseCommand pRequest)
        {
            var response = await _customerService.GetList(pRequest);

            return StatusCode(response.Code, response);
        }

        /// <summary>
        /// Lấy thông tin khách hàng theo id
        /// </summary>
        /// <remarks>
        /// Ràng buộc: 
        /// - Id: int, required
        /// </remarks>
        [HttpGet("detail")]
        [Permission("customer.view")]
        public async Task<ActionResult> Get([FromQuery] DetailBaseCommand pRequest)
        {
            var response = await _customerService.Detail(pRequest);

            return StatusCode(response.Code, response);
        }

        /// <summary>
        /// Sửa thông tin khách hàng
        /// </summary>
        /// <remarks>
        /// Ràng buộc: 
        /// - Id: int, required
        /// </remarks>
        [HttpPut]
        [Permission("customer.update")]
        public async Task<ActionResult> Put([FromBody] UpdateCustomerRequest pRequest)
        {
            var response = await _customerService.Update(pRequest);

            return StatusCode(response.Code, response);
        }



        /// <summary>
        /// Xóa khách hàng - xóa mềm
        /// </summary>
        /// <remarks>
        /// Ràng buộc: 
        /// - Id: int, required
        /// - Xóa User trước rồi mới xóa Customer
        /// </remarks>
        [HttpDelete]
        [Permission("customer.delete")]
        public async Task<ActionResult> Delete([FromBody] UpdateCustomerRequest pRequest)
        {
            var response = await _customerService.Update(pRequest);

            return StatusCode(response.Code, response);
        }
    }
}
