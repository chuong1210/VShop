namespace api_be.Common.Interfaces
{
    public interface IPermissionService
    {
        Task Create(List<string> pPermissions);
    }
}
