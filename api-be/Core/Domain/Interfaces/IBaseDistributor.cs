namespace api_be.Core.Domain.Interfaces
{
    public interface IBaseDistributor
    {
        public string? InternalCode { get; set; }

        public string? Name { get; set; }

        public string? Email { get; set; }

        public string? Phone { get; set; }

        public string? Address { get; set; }
    }
}
