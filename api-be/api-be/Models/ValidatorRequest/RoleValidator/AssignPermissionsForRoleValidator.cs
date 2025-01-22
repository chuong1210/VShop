
using api_be.Domain.Interfaces;
using api_be.Models.Request.RoleRequest;
using api_be.Transforms;

namespace api_be.Models.ValidatorRequest.RoleValidator
{
    public class AssignPermissionsForRoleValidator : AbstractValidator<AssignPermissionsForRoleRequest>
    {
        public AssignPermissionsForRoleValidator(ISupermarketDbContext pContext)
        {
            RuleFor(x => x.RoleId)
                .MustAsync(async (userId, token) =>
                {
                    return await pContext.Roles.FindAsync(userId) != null;
                }).WithMessage(ValidatorTransform.NotExists(Modules.Role.Module));

            RuleFor(x => x.PermissionsName)
                .MustAsync(async (permissionsName, token) =>
                {
                    if (permissionsName != null)
                    {
                        foreach (var permessionName in permissionsName)
                        {
                            var exists = await pContext.Permissions
                                    .FirstOrDefaultAsync(x => x.Name == permessionName) == null;
                            if (exists)
                            {
                                return false;
                            }
                        }
                    }
                    return true;
                }).WithMessage(ValidatorTransform.NotExists(Modules.Permission.Module));
        }
    }
}
