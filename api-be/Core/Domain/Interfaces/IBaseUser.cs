namespace api_be.Core.Domain.Interfaces
{
    public interface IBaseUser
    {
        public string? Email { get; set; }

        public string? PhoneNumber { get; set; }
    }
}
