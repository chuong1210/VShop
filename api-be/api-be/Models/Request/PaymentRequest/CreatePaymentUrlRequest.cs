namespace api_be.Models.Request.PaymentRequest
{
    public record CreatePaymentUrlRequest
    {
        public double? Amount { get; set; }
        public string? Description { get; set; }
    }
}
