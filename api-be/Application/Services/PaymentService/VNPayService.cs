using VNPAY.NET.Enums;
using VNPAY.NET.Models;
using VNPAY.NET;
using Microsoft.Extensions.Options;
using Twilio.Rest.Api.V2010.Account.Conference;
using api_be.Application.Models.ValidatorRequest.PaymentValidator;
using api_be.Application.Responses;
using api_be.Application.Models.ValidatorRequest.PaymentValidator.BasePayment;
using Microsoft.EntityFrameworkCore;
using api_be.Application.Models.Request.PaymentRequest;
using Microsoft.AspNetCore.Http;
using api_be.Domain.ResultResponses;
using api_be.Application.Models.Common;
using api_be.Middleware;
using Microsoft.Extensions.DependencyInjection;

namespace api_be.Application.Services.PaymentService
{
    [RegisterService(ServiceLifetime.Scoped)]

    public class VNPayService : IVNPayService
    {
        private readonly IVnpay _vnpay;
        private readonly VNPayConfig _config;

        public VNPayService(IOptions<VNPayConfig> config)
        {
            _config = config?.Value ?? throw new ArgumentNullException(nameof(config));

            if (string.IsNullOrEmpty(_config.TmnCode) ||
                string.IsNullOrEmpty(_config.HashSecret) ||
                string.IsNullOrEmpty(_config.BaseUrl) ||
                string.IsNullOrEmpty(_config.ReturnUrl))
            {
                throw new InvalidOperationException("VNPay configuration is missing required values.");
            }
            _vnpay = new Vnpay();
            _vnpay.Initialize(_config.TmnCode, _config.HashSecret, _config.BaseUrl, _config.ReturnUrl);
        }

        public async Task<Result<string>> CreatePaymentUrlAsync(CreatePaymentUrlRequest request, string ipAddress)
        {
            var validator = new CreatePaymentUrlValidator();
            var validationResult = await validator.ValidateAsync(request);

            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return Result<string>.Failure(errors, StatusCodes.Status400BadRequest);
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
            return Result<string>.Success(uri, StatusCodes.Status201Created);
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
