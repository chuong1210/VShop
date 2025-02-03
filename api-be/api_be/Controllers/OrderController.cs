using api_be.Domain.Exceptions;
using api_be.Middleware;
using api_be.Domain.Models.Request.OrderRequest;
using api_be.Domain.Models.Responses;
using api_be.Application.Services;
using api_be.Application.Services.Imps;
using api_be.Domain.DefaultValidatorBase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api_be.Controllers
{
    [Route("~/smw-api/[controller]")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrderController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        /// <summary>
        /// Lấy danh sách đơn hàng
        /// </summary>
        /// <remarks>
        /// Ràng buộc: 
        ///
        /// </remarks>
        [HttpGet]
        [Permission("order.view")]
        public async Task<ActionResult> Get([FromQuery] ListBaseCommand pRequest)
        {
            var response = await _orderService.GetList(pRequest);

            return StatusCode(response.Code, response);
        }

        /// <summary>
        /// Lấy thông tin đơn hàng theo id
        /// </summary>
        /// <remarks>
        /// Ràng buộc: 
        /// - Id: int, required
        /// </remarks>
        [HttpGet("detail")]
        [Permission("order.view")]
        public async Task<ActionResult> GetOrder([FromQuery] DetailBaseCommand pRequest)
        {
            var response = await _orderService.DetailOrder(pRequest);

            return StatusCode(response.Code, response);
        }

        /// <summary>
        /// Lấy thông tin chi tiết giỏ hàng của người dùng
        /// </summary>
        /// <remarks>
        /// Ràng buộc: 
        /// </remarks>
        [HttpGet("detail-cart")]
        public async Task<ActionResult> GetCart([FromQuery] DetailBaseCommand pRequest)
        {
            var response = await _orderService.DetailCart();

            return StatusCode(response.Code, response);
        }


        /// <summary>
        /// Thêm sản phẩm vào giỏ hàng
        /// </summary>
        /// <remarks>
        /// Ràng buộc:
        /// - ProductId: Id của sản phẩm
        /// - Quantity: > 0
        /// </remarks>
        [HttpPost("cart")]
        [AllowAnonymous]
        public async Task<ActionResult> Post([FromBody] AddProductToCartRequest pRequest)
        {
            var response = await _orderService.AddProductToCart(pRequest);

            return StatusCode(response.Code, response);
        }

        /// <summary>
        /// Xoá sản phẩm khỏi giỏ hàng
        /// </summary>
        /// <remarks>
        /// Ràng buộc:
        /// - ProductId: Id của sản phẩm trong đơn hàng
        /// </remarks>
        [HttpPost("cart-remove")]
        [AllowAnonymous]
        public async Task<ActionResult> Post([FromBody] RemoveProductInCartRequest pRequest)
        {
            var response = await _orderService.RemoveProductInCart(pRequest);

            return StatusCode(response.Code, response);
        }

        /// <summary>
        /// Cập nhật số lượng của sản phẩm trong giỏ hàng
        /// </summary>
        /// <remarks>
        /// Ràng buộc:
        /// - ProductId: Id của sản phẩm
        /// - Quantity: > 0
        /// </remarks>
        [HttpPut("cart")]
        [AllowAnonymous]
        public async Task<ActionResult> Put([FromBody] UpdateProductInCartRequest pRequest)
        {
            var response = await _orderService.UpdateProductInCart(pRequest);

            return StatusCode(response.Code, response);
        }

        /// <summary>
        /// Áp dụng chương trình khuyến mãi
        /// </summary>
        /// <remarks>
        /// Ràng buộc:
        /// - InternalCodeCoupon: Mã chương trình khuyến mãi
        /// </remarks>
        [HttpPost("cart-add-coupon")]
        [AllowAnonymous]
        public async Task<ActionResult> AddCoupon([FromBody] AddCouponToCartRequest pRequest)
        {
            var response = await _orderService.AddCouponToCart(pRequest);

            return StatusCode(response.Code, response);
        }

        /// <summary>
        /// Đặt hàng
        /// </summary>
        /// <remarks>
        /// Ràng buộc:
        /// - ProductsId: Id của sản phẩm trong giỏ hàng
        /// - Message: Lời nhắn cho cửa hàng khi đặt hàng
        /// </remarks>
        [HttpPost("order-create")]
        [AllowAnonymous]
        public async Task<ActionResult> Post([FromBody] CreateOrderRequest pRequest)
        {
            var response = await _orderService.CreateOrder(pRequest);

            return StatusCode(response.Code, response);
        }

        /// <summary>
        /// Huỷ đơn đặt hàng
        /// </summary>
        /// <remarks>
        /// Ràng buộc:
        /// - OrderId: Id của đơn hàng người dùng
        /// </remarks>
        [HttpPatch("order-cancel")]
        [AllowAnonymous]
        public async Task<ActionResult> Put([FromBody] CancelOrderRequest pRequest)
        {
            var response = await _orderService.CancelOrder(pRequest);

            return StatusCode(response.Code, response);
        }

        /// <summary>
        /// Nhận viên thay đổi trạng thái đơn hàng
        /// </summary>
        /// <remarks>
        /// Ràng buộc:
        /// - OrderId: Id của đơn hàng
        /// - Status: Trạng thái thay đổi của đơn hàng
        /// Cart(0), Order(1), Approve(2), Transport(3), Received(4), Cancel(5)
        /// </remarks>
        [HttpPatch("order-change-status")]
        [Permission("order.change-status")]
        public async Task<ActionResult> Change([FromBody] ChangeStatusOrderRequest pRequest)
        {
            var response = await _orderService.ChangeStatusOrder(pRequest);

            return StatusCode(response.Code, response);
        }


    }
}
