namespace api_be.Application.Models.Request.OrderRequest
{
    public record AddCouponToCartRequest
    {
        public string? InternalCodeCoupon { get; set; }

    }
}
