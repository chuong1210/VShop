using VNPAY.NET.Models;

namespace api_be.Services
{
    public interface IVNPayService
    {
        string CreatePaymentUrl(double amount, string description, string ipAddress);
        PaymentResult HandleIpnAction(IQueryCollection query);
        PaymentResult HandleCallback(IQueryCollection query);
}
}
