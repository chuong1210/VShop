using api_be.Application.Models.Request.SupplierOrderRequest;
using api_be.Application.Models.ValidatorRequest.DefaultValidatorBase;
using api_be.Application.Services;
using api_be.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace api_be.API.Controllers
{
    [Route("~/smw-api/supplier-order")]
    [ApiController]
    public class SupplierOrderController : ControllerBase
    {
        private readonly ISupplierOrderService _supplierOrderService;

        public SupplierOrderController(ISupplierOrderService supplierOrderService)
        {
            _supplierOrderService = supplierOrderService;
        }

        /// <summary>
        /// Lấy danh sách sản phẩm yêu cầu nhập
        /// </summary>
        /// <remarks>
        /// Ràng buộc: 
        ///
        /// </remarks>
        [HttpGet]
        //[Permission("supplier-order.view")]
        public async Task<ActionResult> Get([FromQuery] ListBaseCommand pRequest)
        {
            var response = await _supplierOrderService.GetList(pRequest);

            return StatusCode(response.Code, response);
        }

        /// <summary>
        /// Lấy thông tin danh sách sản phẩm yêu cầu nhập
        /// </summary>
        /// <remarks>
        /// Ràng buộc: 
        /// - Id: int, required
        /// </remarks>
        [HttpGet("detail")]
        //[Permission("supplier-order.view")]
        public async Task<ActionResult> Get([FromQuery] DetailBaseCommand pRequest)
        {
            var response = await _supplierOrderService.Detail(pRequest);

            return StatusCode(response.Code, response);
        }

        /// <summary>
        /// Lấy danh sách sản phẩm theo id đơn nhập
        /// </summary>
        /// <remarks>
        /// Ràng buộc: 
        /// - Id: int, required
        /// </remarks>
        [HttpGet("list-product")]
        //[Permission("supplier-order.view")]
        public async Task<ActionResult> GetProduct([FromQuery] DetailBaseCommand pRequest)
        {
            var response = await _supplierOrderService.ProductSupplierOrder(pRequest);

            return StatusCode(response.Code, response);
        }

        /// <summary>
        /// Tạo danh sách sản phẩm đặt hàng
        /// </summary>
        /// <remarks>
        /// Ràng buộc:
        /// - ReceivingStaffId: Id người nhận hàng từ bảng Staff
        /// - DistributorId: Id nhà cung cấp từ Distributor
        /// - Details: Danh sách sản phẩm
        ///     + ProductId: Id sản phẩm từ bảng Product
        ///     + Quantity: > 0
        ///     + Price: > 0
        /// </remarks>
        [HttpPost]
        //[Permission("supplier-order.create")]
        public async Task<ActionResult> Post([FromBody] CreateOrUpdateSupplierOrderRequest pRequest)
        {
            var response = await _supplierOrderService.Create(pRequest);

            return StatusCode(response.Code, response);
        }

        /// <summary>
        /// Cập nhật danh sách sản phẩm đặt hàng
        /// </summary>
        /// <remarks>
        /// Ràng buộc:
        /// - ReceivingStaffId: Id người nhận hàng từ bảng Staff
        /// - DistributorId: Id nhà cung cấp từ Distributor
        /// - Details: Danh sách sản phẩm
        ///     + ProductId: Id sản phẩm từ bảng Product
        ///     + Quantity: > 0
        ///     + Price: > 0
        /// </remarks>
        [HttpPut]
        //[Permission("supplier-order.update")]
        public async Task<ActionResult> Put([FromBody] CreateOrUpdateSupplierOrderRequest pRequest)
        {
            var response = await _supplierOrderService.Update(pRequest);

            return StatusCode(response.Code, response);
        }

        /// <summary>
        /// Nhận viên thay đổi trạng thái danh sách sản phẩm nhập hàng
        /// </summary>
        /// <remarks>
        /// Ràng buộc:
        /// - SupplierOrderId: Id của danh sách sản phẩm nhập
        /// - Status: Trạng thái thay đổi của ds nhập
        /// Draft(0), Order(1), Cancel(2)
        /// </remarks>
        [HttpPatch("change-status")]
        //[Permission("supplier-order.change-status")]
        public async Task<ActionResult> ChangeStatus([FromBody] ChangeStatusSupplierOrderRequest pRequest)
        {
            var response = await _supplierOrderService.ChangeStatus(pRequest);

            return StatusCode(response.Code, response);
        }
    }
}
