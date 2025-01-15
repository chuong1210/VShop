using Microsoft.AspNetCore.Authorization;

namespace api_be.Middleware
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class RoleAttribute : AuthorizeAttribute
    {
        public RoleAttribute(string role) : base("RolePolicy")
        {
            Policy = $"Role_{role}";
        }
    }
}
