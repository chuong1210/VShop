using api_be.Services;
using Microsoft.AspNetCore.Mvc;

namespace api_be.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymenController:ControllerBase
    {
        private readonly IVNPayService _vnpayService;

        public PaymenController(IVNPayService vnpayService)
        {
            _vnpayService = vnpayService;
        }

        // Tạo URL thanh toán
        [HttpGet("CreatePaymentUrl")]
        public ActionResult<string> CreatePaymentUrl([FromQuery] double amount, [FromQuery] string description)
        {
            try
            {
                var ipAddress = Request.HttpContext.Connection.RemoteIpAddress?.ToString();
                var paymentUrl = _vnpayService.CreatePaymentUrl(amount, description, ipAddress);
                return Ok(paymentUrl);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // Xử lý IPN từ VNPAY
        [HttpGet("IpnAction")]
        public IActionResult IpnAction()
        {
            if (Request.QueryString.HasValue)
            {
                try
                {
                    var result = _vnpayService.HandleIpnAction(Request.Query);
                    if (result.IsSuccess)
                    {
                        // Xử lý đơn hàng khi thanh toán thành công
                        return Ok("Payment successful");
                    }
                    return BadRequest("Payment failed");
                }
                catch (Exception ex)
                {
                    return BadRequest(ex.Message);
                }
            }
            return NotFound("No payment information found");
        }

        // Xử lý Callback từ VNPAY
        [HttpGet("Callback")]
        public ActionResult<string> Callback()
        {
            if (Request.QueryString.HasValue)
            {
                try
                {
                    var result = _vnpayService.HandleCallback(Request.Query);
                    var message = $"{result.PaymentResponse.Description}. {result.TransactionStatus.Description}.";

                    if (result.IsSuccess)
                    {
                        return Ok(message);
                    }

                    return BadRequest(message);
                }
                catch (Exception ex)
                {
                    return BadRequest(ex.Message);
                }
            }
            return NotFound("No payment information found");
        }
    }
}
