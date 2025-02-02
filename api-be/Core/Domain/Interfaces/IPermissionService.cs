namespace api_be.Core.Domain.Interfaces
{
    public interface IPermissionService
    {
        Task Create(List<string> pPermissions);
    }
}
