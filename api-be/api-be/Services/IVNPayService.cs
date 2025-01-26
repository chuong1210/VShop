using api_be.Models.Request.PaymentRequest;
using api_be.Models.Responses;
using VNPAY.NET.Models;

namespace api_be.Services
{
    public interface IVNPayService
    {
        Result<Task<string>> CreatePaymentUrlAsync(CreatePaymentUrlRequest request, string ipAddress);
        PaymentResult HandleIpnAction(IQueryCollection query);
        PaymentResult HandleCallback(IQueryCollection query);
}
}
