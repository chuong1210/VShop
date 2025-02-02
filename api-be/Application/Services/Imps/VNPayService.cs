using api_be.Domain.Models.Common;
using VNPAY.NET.Enums;
using VNPAY.NET.Models;
using VNPAY.NET;
using Microsoft.Extensions.Options;
using Twilio.Rest.Api.V2010.Account.Conference;
using api_be.Application.ValidatorRequest.PaymentValidator;
using api_be.Domain.Models.Responses;
using api_be.Application.ValidatorRequest.PaymentValidator.BasePayment;
using Microsoft.EntityFrameworkCore;
using api_be.Domain.Models.Request.PaymentRequest;
using Microsoft.AspNetCore.Http;

namespace api_be.Application.Services.Imps
{
    public class VNPayService:IVNPayService
    {
        private readonly IVnpay _vnpay;
        private readonly VNPayConfig _config;

        public VNPayService(IOptions<VNPayConfig> config)
        {
            _config = config.Value;
            _vnpay = new Vnpay();
            _vnpay.Initialize(_config.TmnCode, _config.HashSecret, _config.BaseUrl, _config.ReturnUrl);
        }

        public async Task<Result<String>> CreatePaymentUrlAsync(CreatePaymentUrlRequest request, string ipAddress)
        {
            var validator = new CreatePaymentUrlValidator();
            var validationResult = await validator.ValidateAsync(request);

            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return Result<String>.Failure(errors, StatusCodes.Status400BadRequest);
            }
            var paymentRequest = new PaymentRequest
            {
                PaymentId = DateTime.Now.Ticks,
                Money = request.Amount ?? 0,
                Description = request.Description ?? "",
                IpAddress = ipAddress,
                BankCode = BankCode.ANY,
                CreatedDate = DateTime.Now,
                Currency = Currency.VND,
                Language = DisplayLanguage.Vietnamese
            };

            var uri = _vnpay.GetPaymentUrl(paymentRequest);
            return( Result<String>.Success(uri, StatusCodes.Status201Created));
        }

        public PaymentResult HandleIpnAction(IQueryCollection query)
        {
            return _vnpay.GetPaymentResult(query);
        }

        public PaymentResult HandleCallback(IQueryCollection query)
        {
            return _vnpay.GetPaymentResult(query);
        }

    }
}
