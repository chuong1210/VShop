namespace api_be.Core.Domain.Interfaces
{
    public interface IBaseStaffPosition
    {
        public string? InternalCode { get; set; }

        public string? Name { get; set; }

        public string? Describes { get; set; }

        public List<int?>? Roles { get; set; }
    }
}
