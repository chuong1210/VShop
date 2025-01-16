namespace api_be.Models.Responses
{
    public class LoginDto
    {
        public int Id { get; set; }

        public DateTime? Exp { get; set; }

        public string? Token { get; set; }
        public string? RefreshToken { get; set; }

    }
}
