namespace api_be.Core.Domain.Interfaces
{
    public interface IBasePayment
    {
        public string? InternalCode { get; set; }

        public string? Name { get; set; }
    }
}
