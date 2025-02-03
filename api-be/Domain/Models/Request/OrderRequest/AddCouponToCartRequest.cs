namespace api_be.Domain.Models.Request.OrderRequest
{
    public record AddCouponToCartRequest
    {
        public string? InternalCodeCoupon { get; set; }

    }
}
