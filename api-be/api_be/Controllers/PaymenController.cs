using api_be.Domain.Exceptions;
using api_be.Middleware;
using api_be.Domain.Models.Request.PaymentRequest;
using api_be.Domain.Models.Responses;
using api_be.Application.Services;
using api_be.Domain.DefaultValidatorBase;
using Microsoft.AspNetCore.Mvc;

namespace api_be.Controllers
{
    [ApiController]
    [Route("~/smw-api/[controller]")]
    public class PaymenController:ControllerBase
    {
        private readonly IVNPayService _vnpayService;
        private readonly IPaymentService _paymentService;
        public PaymenController(IVNPayService vnpayService, IPaymentService paymentService)
        {
            _vnpayService = vnpayService;
            _paymentService = paymentService;
        }
        /// <summary>
        /// Lấy danh sách phương thức thanh toán
        /// </summary>
        /// <remarks>
        /// Ràng buộc: 
        ///
        /// </remarks>
        [HttpGet]
        [Permission("payment.view")]
        public async Task<ActionResult> Get([FromQuery] ListBaseCommand pRequest)
        {
            var response = await _paymentService.GetList (pRequest);

            return StatusCode(response.Code, response);
        }

        /// <summary>
        /// Lấy thông tin PT thanh toán theo id
        /// </summary>
        /// <remarks>
        /// Ràng buộc: 
        /// - Id: int, required
        /// </remarks>
        [HttpGet("detail")]
        [Permission("payment.view")]
        public async Task<ActionResult> Get([FromQuery] DetailBaseCommand pRequest)
        {
            var response = await _paymentService.Detail(pRequest);

            return StatusCode(response.Code, response);
        }

        /// <summary>
        /// Thêm mới phương thức thanh toán
        /// </summary>
        /// <remarks>
        /// Ràng buộc:
        /// - InternalCode: string, required, max(50)
        /// - Name: string, required, max(190)
        /// </remarks>
        [HttpPost]
        [Permission("payment.create")]
        public async Task<ActionResult> Post([FromBody] CreateOrUpdatePaymentRequest pRequest)
        {
            var response = await _paymentService.Create(pRequest);

            return StatusCode(response.Code, response);
        }

        /// <summary>
        /// Sửa thông tin phương thức thanh toán
        /// </summary>
        /// <remarks>
        /// Ràng buộc: 
        /// - Id: int, required
        /// </remarks>
        [HttpPut]
        [Permission("payment.update")]
        public async Task<ActionResult> Put([FromBody] CreateOrUpdatePaymentRequest pRequest)
        {
            var response = await _paymentService.Update(pRequest);

            return StatusCode(response.Code, response);
        }

        /// <summary>
        /// Xóa phương thức thanh toán
        /// </summary>
        /// <remarks>
        /// Ràng buộc: 
        /// - Id: int, required
        /// </remarks>
        [HttpDelete]
        [Permission("payment.delete")]
        public async Task<ActionResult> Delete([FromQuery] int pRequest)
        {
            try
            {
                var response = await _paymentService.Delete(pRequest);
                return StatusCode(StatusCodes.Status204NoContent);
            }
            catch (NotFoundException ex)
            {
                var responses = Result<PaymentDto>.Failure(ex.Message, StatusCodes.Status404NotFound);
                return StatusCode(responses.Code, responses);
            }
            catch (BadRequestException ex)
            {
                var responses = Result<PaymentDto>.Failure(ex.Message, StatusCodes.Status400BadRequest);
                return StatusCode(responses.Code, responses);
            }
            catch (Exception ex)
            {
                var responses = Result<PaymentDto>.Failure(ex.Message, StatusCodes.Status500InternalServerError);
                return StatusCode(responses.Code, responses);
            }

    }



        // Tạo URL thanh toán
        [HttpGet("CreatePaymentUrl")]
        public async Task<ActionResult> CreatePaymentUrl([FromQuery] CreatePaymentUrlRequest request)
        {
            try
            {
                var ipAddress = Request.HttpContext.Connection.RemoteIpAddress?.ToString();
                var response = _vnpayService.CreatePaymentUrlAsync(request, ipAddress).Result;
                return StatusCode(response.Code, response);

            }
            catch (Exception ex)
            {
                throw new BadRequestException(ex.Message);
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
                    throw new BadRequestException("Payment failed");
                }
                catch (Exception ex)
                {
                    throw new BadRequestException(ex.Message);
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

                    throw new BadRequestException(message);
                }
                catch (Exception ex)
                {
                    throw new BadRequestException(ex.Message);
                }
            }
            return NotFound("No payment information found");
        }
    }
}
