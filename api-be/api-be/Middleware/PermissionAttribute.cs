using Microsoft.AspNetCore.Authorization;

namespace api_be.Middleware
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class PermissionAttribute : AuthorizeAttribute
    {
        public PermissionAttribute(string permission) : base("PermissionPolicy")
        {
            Policy = $"{permission}";
        }
    }

}
