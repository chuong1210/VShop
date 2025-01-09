namespace api_be.Interfaces
{
    public interface IBaseUser
    {
        public string? Email { get; set; }

        public string? PhoneNumber { get; set; }
    }
}
