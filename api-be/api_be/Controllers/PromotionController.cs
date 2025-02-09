using api_be.Domain.Exceptions;
using api_be.Middleware;
using api_be.Application.Models.Request;
using api_be.Application.Models.Request.PromotionRequest;
using api_be.Application.Responses;
using api_be.Application.Services;
using api_be.Domain.Transforms;
using Azure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using api_be.Application.Models.ValidatorRequest.DefaultValidatorBase;

namespace api_be.API.Controllers
{
    [Route("~/smw-api/[controller]")]
    [ApiController]
    public class PromotionController : ControllerBase
    {
        private readonly IPromotionService _promotionService;

        public PromotionController(IPromotionService promotion)
        {
            _promotionService = promotion;
        }

        /// <summary>
        /// Lấy danh sách chương trình khuyến mãi
        /// </summary>
        /// <remarks>
        /// Ràng buộc: 
        ///
        /// </remarks>
        [HttpGet]
        [Permission("promotion.view")]
        public async Task<ActionResult> Get([FromQuery] ListBaseCommand pRequest)
        {
            var response = await _promotionService.GetList(pRequest);

            return StatusCode(response.Code, response);
        }

        /// <summary>
        /// Lấy thông tin chi tiết CTKM theo id
        /// </summary>
        /// <remarks>
        /// Ràng buộc: 
        /// - Id: int, required
        /// </remarks>
        [HttpGet("detail")]
        [Permission("promotion.view")]
        public async Task<ActionResult> Get([FromQuery] DetailBaseCommand pRequest)
        {
            var response = await _promotionService.Detail(pRequest);

            return StatusCode(response.Code, response);
        }

        /// <summary>
        /// Thêm mới CTKM
        /// </summary>
        /// <remarks>
        /// Ràng buộc:
        /// - InternalCode: string, required, max(50)
        /// - Name: string, required, max(190)
        /// - Start: > thời gian hiện tại
        /// - End: > thời gian bắt đầu
        /// - Limit: > 0
        /// - Type: Discount(0)
        ///     + Discount: > 0
        ///     + PercentMax: (0, 100]
        /// - Type: Percent(1)
        ///     + Percent: (0, 100]
        ///     + DiscountMax: > 0
        /// </remarks>
        [HttpPost]
        [Permission("promotion.create")]
        public async Task<ActionResult> Post([FromBody] CreateOrUpdatePromotionRequest pRequest)
        {
            var response = await _promotionService.Create(pRequest);

            return StatusCode(response.Code, response);
        }

        /// <summary>
        /// Sửa thông tin CTKM
        /// </summary>
        /// <remarks>
        /// Ràng buộc: 
        /// - Id: int, required
        /// - Thông tin chỉnh sửa tương tự thông tin nhập
        /// </remarks>
        [HttpPut]
        [Permission("promotion.update")]
        public async Task<ActionResult> Put([FromBody] CreateOrUpdatePromotionRequest pRequest)
        {
            var response = await _promotionService.Update(pRequest);

            return StatusCode(response.Code, response);
        }

        /// <summary>
        /// Thay đổi trạng thái CTKM
        /// </summary>
        /// <remarks>
        /// Ràng buộc: 
        /// - promotionId: int, required
        /// - status:
        ///     + Nháp(0) => Duyệt(1)/Huỷ bỏ(2)
        ///     + Duyệt(1) => Nháp(0)/Huỷ bỏ(2)
        ///     + Huỷ bỏ(2) => Không được thay đổi
        /// </remarks>
        [HttpPatch]
        [Permission("promotion.change-status")]
        public async Task<ActionResult> ChangeStatus([FromBody] ChangeStatusPromotionRequest pRequest)
        {
            var response = await _promotionService.ChangeStatus(pRequest);

            return StatusCode(response.Code, response);
        }

        /// <summary>
        /// Áp dụng CTKM cho nhóm sản phẩm
        /// </summary>
        /// <remarks>
        /// Ràng buộc:
        /// - PromotionId: Id của CTKM được áp dụng
        /// - ProductsId: List Id của sản phẩm muốn áp dụng CTKM này
        /// - Group:
        ///     + -1: Nếu tạo mới
        ///     + Ngược lại là Group của nhóm cần cập nhật danh sách sản phẩm áp dụng
        /// </remarks>
        [HttpPost("apply")]
        [Permission("promotion.apply")]
        public async Task<ActionResult> Apply([FromBody] ApplyPromotionForProductRequest pRequest)
        {
            var response = await _promotionService.ApplyPromotionForProduct(pRequest);

            return StatusCode(response.Code, response);
        }
    }
}
