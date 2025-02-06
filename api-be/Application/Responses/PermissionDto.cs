namespace api_be.Application.Responses
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
