namespace api_be.Domain.Interfaces
{
    public interface IPermissionService
    {
        Task Create(List<string> pPermissions);
    }
}
