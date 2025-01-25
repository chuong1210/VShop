using api_be.Models.Common;
using VNPAY.NET.Enums;
using VNPAY.NET.Models;
using VNPAY.NET;
using Microsoft.Extensions.Options;

namespace api_be.Services.Imps
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

        public string CreatePaymentUrl(double amount, string description, string ipAddress)
        {
            var paymentRequest = new PaymentRequest
            {
                PaymentId = DateTime.Now.Ticks,
                Money = amount,
                Description = description,
                IpAddress = ipAddress,
                BankCode = BankCode.ANY,
                CreatedDate = DateTime.Now,
                Currency = Currency.VND,
                Language = DisplayLanguage.Vietnamese
            };

            return _vnpay.GetPaymentUrl(paymentRequest);
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
