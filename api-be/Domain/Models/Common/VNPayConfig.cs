namespace api_be.Domain.Models.Common
{
    public record VNPayConfig
    {
        public string TmnCode { get; set; }
        public string HashSecret { get; set; }
        public string BaseUrl { get; set; }
        public string ReturnUrl { get; set; }
    }
}
