

using api_be.Middleware;
using api_be.Application.Models.Request.DeliveryRequest;
using api_be.Application.Services;
using Microsoft.AspNetCore.Mvc;
using api_be.Domain.ResultResponses;
using api_be.Application.Models.ValidatorRequest.DefaultValidatorBase;
using api_be.Application.Models.Request.ImportGoodRequest;


namespace api_be.Controllers
{
    [Route("~/smw-api/import-goods")]
    [ApiController]
    public class ImportGoodsController : ControllerBase
    {
        private readonly IImportGoodsService _importGoodsService;

        public ImportGoodsController(IImportGoodsService mediator)
        {
            _importGoodsService = mediator;
        }

        /// <summary>
        /// Lấy danh sách hoá đơn nhập hàng
        /// </summary>
        /// <remarks>
        /// Ràng buộc: 
        ///
        /// </remarks>
        [HttpGet]
        [Permission("import-good.view")]
        public async Task<ActionResult> Get([FromQuery] ListBaseCommand pRequest)
        {
            var response = await _importGoodsService.GetList(pRequest);

            return StatusCode(response.Code, response);
        }

        /// <summary>
        /// Lấy thông tin hoá đơn nhập hàng theo id
        /// </summary>
        /// <remarks>
        /// Ràng buộc: 
        /// - Id: int, required
        /// </remarks>
        [HttpGet("detail")]
        [Permission("import-good.view")]
        public async Task<ActionResult> Get([FromQuery] DetailBaseCommand pRequest)
        {
            var response = await _importGoodsService.Detail(pRequest);

            return StatusCode(response.Code, response);
        }

        /// <summary>
        /// Nhập hàng từ nhà cung cấp khi họ giao hàng tới - Nháp
        /// </summary>
        /// <remarks>
        /// Ràng buộc:
        /// - SupplierOrderId: Id từ đơn đặt hàng nào trong bảng SupplierOrder
        /// - Details: Danh sách sản phẩm
        ///     + ProductId: Id sản phẩm từ bảng Product
        ///     + Quantity: > 0
        /// </remarks>
        [HttpPost]
        [Permission("import-good.create")]
        public async Task<ActionResult> Post([FromBody] CreateImportGoodsRequest pRequest)
        {
            var response = await _importGoodsService.Create(pRequest);

            return StatusCode(response.Code, response);
        }

        /// <summary>
        /// Cập nhật nhập hàng từ nhà cung cấp khi họ giao hàng tới
        /// </summary>
        /// <remarks>
        /// Ràng buộc:
        /// - SupplierOrderId: Id từ đơn đặt hàng nào trong bảng SupplierOrder
        /// - Details: Danh sách sản phẩm
        ///     + ProductId: Id sản phẩm từ bảng Product
        ///     + Quantity: > 0
        /// </remarks>
        [HttpPut]
        [Permission("import-good.update")]
        public async Task<ActionResult> Put([FromBody] UpdateImportGoodsRequest pRequest)
        {
            var response = await _importGoodsService.Update(pRequest);

            return StatusCode(response.Code, response);
        }

        /// <summary>
        /// Nhận viên thay đổi trạng thái danh sách sản phẩm nhập hàng
        /// </summary>
        /// <remarks>
        /// Ràng buộc:
        /// - SupplierOrderId: Id của đơn nhập hàng
        /// - IsCancel: true - Huỷ đơn
        /// /// - IsCancel: false - Xác nhận nhập hàng vào kho
        /// </remarks>
        [HttpPatch]
        [Permission("import-good.change-status")]
        public async Task<ActionResult> ChangeStatus([FromBody] ChangeStatusImportGoodsRequest pRequest)
        {
            var response = await _importGoodsService.ChangeStatus(pRequest);

            return StatusCode(response.Code, response);
        }
    }
}
