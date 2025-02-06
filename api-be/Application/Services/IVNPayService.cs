using api_be.Application.Models.Request.PaymentRequest;
using api_be.Application.Responses;
using Microsoft.AspNetCore.Http;
using VNPAY.NET.Models;
using api_be.Domain.ResultResponses;

namespace api_be.Application.Services
{
    public interface IVNPayService
    {
        Task<Result<string>> CreatePaymentUrlAsync(CreatePaymentUrlRequest request, string ipAddress);
        PaymentResult HandleIpnAction(IQueryCollection query);
        PaymentResult HandleCallback(IQueryCollection query);
}
}
