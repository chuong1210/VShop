namespace api_be.Models.Request.OrderRequest
{
    public record AddCouponToCartRequest
    {
        public string? InternalCodeCoupon { get; set; }

    }
}
