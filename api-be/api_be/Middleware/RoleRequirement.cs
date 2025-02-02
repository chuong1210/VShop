using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace api_be.Middleware
{
    public class RoleRequirement : IAuthorizationRequirement
    {
        public string Role { get; }

        public RoleRequirement(string role)
        {
            Role = role;
        }
    }

    public class RoleRequirementHandler : AuthorizationHandler<RoleRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, RoleRequirement requirement)
        {
            // Kiểm tra nếu người dùng có claim "role" phù hợp
            if (context.User.HasClaim(c => c.Type == ClaimTypes.Role && c.Value == requirement.Role))
            {
                context.Succeed(requirement); // Cấp quyền nếu vai trò phù hợp
            }
            else
            {
                context.Fail(); // Từ chối truy cập nếu không có vai trò
            }

            return Task.CompletedTask;
        }
    }
}
