

using api_be.Middleware;
using api_be.Domain.Models.Request.DeliveryRequest;
using api_be.Domain.DefaultValidatorBase;
using api_be.Application.Services;
using api_be.Domain.DefaultValidatorBase;
using Microsoft.AspNetCore.Mvc;

namespace api_be.Controllers
{
    [Route("~/smw-api/[controller]")]
    [ApiController]
    public class DeliveryController : ControllerBase
    {
        private readonly IDeliveryService _deliveryService;

        public DeliveryController(IDeliveryService mediator)
        {
            _deliveryService = mediator;
        }

        /// <summary>
        /// Lấy danh sách đơn giao hàng
        /// </summary>
        /// <remarks>
        /// Ràng buộc: 
        ///
        /// </remarks>
        [HttpGet]
        [Permission("delivery.view")]
        public async Task<ActionResult> Get([FromQuery] ListBaseCommand pRequest)
        {
            var response = await _deliveryService.GetList(pRequest);

            return StatusCode(response.Code, response);
        }

        /// <summary>
        /// Lấy thông tin chi tiết đơn giao hàng theo id
        /// </summary>
        /// <remarks>
        /// Ràng buộc: 
        /// - Id: int, required
        /// </remarks>
        [HttpGet("detail")]
        [Permission("delivery.view")]
        public async Task<ActionResult> Get([FromQuery] DetailBaseCommand pRequest)
        {
            var response = await _deliveryService.Detail(pRequest);

            return StatusCode(response.Code, response);
        }

        /// <summary>
        /// Thêm mới giao hàng
        /// </summary>
        /// <remarks>
        /// Ràng buộc:
        /// - From: Giao từ đâu
        /// - To: Giao tới đâu
        /// - TransportFee: Phí vận chuyển
        /// </remarks>
        [HttpPost]
        [Permission("delivery.create")]
        public async Task<ActionResult> Post([FromBody] CreateOrUpdateDeliveryRequest pRequest)
        {
            var response = await _deliveryService.Create(pRequest);

            return StatusCode(response.Code, response);
        }

        /// <summary>
        /// Sửa thông tin đơn giao hàng
        /// </summary>
        /// <remarks>
        /// Ràng buộc: 
        /// - Id: int, required
        /// </remarks>
        [HttpPut]
        [Permission("delivery.update")]
        public async Task<ActionResult> Put([FromBody] CreateOrUpdateDeliveryRequest pRequest)
        {
            var response = await _deliveryService.Update(pRequest);

            return StatusCode(response.Code, response);
        }

        /// <summary>
        /// Thay đổi trạng thái đơn giao hàng
        /// </summary>
        /// <remarks>
        /// Ràng buộc:
        /// - DeliveryId: Id của đơn giao hàng
        /// - Status: Trạng thái thay đổi của đơn giao hàng
        /// (0): Prepare (chuẩn bị),
        /// (1): Transport (Vận chuyển)
        /// (2): Delivered (Đã giao hàng)
        /// (3): Received (Nhận hàng)
        /// (4): Cancel (Huỷ)
        /// </remarks>
        [HttpPatch]
        [Permission("delivery.change-status")]
        public async Task<ActionResult> Change([FromBody] ChangeStatusDeliveryRequest pRequest)
        {
            var response = await _deliveryService.ChangeStatus(pRequest);

            return StatusCode(response.Code, response);
        }
    }
}
