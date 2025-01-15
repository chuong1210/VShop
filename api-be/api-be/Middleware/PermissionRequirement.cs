using api_be.Constants;
using Microsoft.AspNetCore.Authorization;

namespace api_be.Middleware
{
    public class PermissionRequirement : IAuthorizationRequirement
    {
        public string Permission { get; }

        public PermissionRequirement(string permission)
        {
            Permission = permission;
        }
    }

    public class PermissionRequirementHandler : AuthorizationHandler<PermissionRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
        {
            if (context.User.HasClaim(c => c.Value == CLAIMS_VALUES.TYPE_ADMIN))
            {
                context.Succeed(requirement);
            }
            else if (context.User.HasClaim(c => c.Type == CONSTANT_CLAIM_TYPES.Permission && c.Value == requirement.Permission))
            {
                context.Succeed(requirement);
            }
            else if(CONSTANT_PERMESSION_DEFAULT.PERMISSIONS_NO_LOGIN.Contains(requirement.Permission))
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }

}
