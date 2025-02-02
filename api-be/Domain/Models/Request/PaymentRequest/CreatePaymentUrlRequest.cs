namespace api_be.Domain.Models.Request.PaymentRequest
{
    public record CreatePaymentUrlRequest
    {
        public double? Amount { get; set; }
        public string? Description { get; set; }
    }
}
