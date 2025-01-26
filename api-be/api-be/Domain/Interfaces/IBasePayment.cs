namespace api_be.Domain.Interfaces
{
    public interface IBasePayment
    {
        public string? InternalCode { get; set; }

        public string? Name { get; set; }
    }
}
