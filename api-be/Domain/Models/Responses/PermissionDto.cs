namespace api_be.Domain.Models.Responses
{
    public record PermissionDto
    {
        public int? Id { get; set; }
        public string? Name
        {
            get; set;
        }
    }
}
