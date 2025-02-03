using api_be.Domain.Models.Request.PaymentRequest;
using api_be.Domain.Models.Responses;
using Microsoft.AspNetCore.Http;
using VNPAY.NET.Models;

namespace api_be.Application.Services
{
    public interface IVNPayService
    {
        Task<Result<string>> CreatePaymentUrlAsync(CreatePaymentUrlRequest request, string ipAddress);
        PaymentResult HandleIpnAction(IQueryCollection query);
        PaymentResult HandleCallback(IQueryCollection query);
}
}
