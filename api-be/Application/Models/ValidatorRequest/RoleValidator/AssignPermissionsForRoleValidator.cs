
using api_be.Core.Domain.Interfaces;
using api_be.Application.Models.Request.RoleRequest;
using api_be.Domain.Transforms;
using api_be.Infrastructure.DB;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace api_be.Application.Models.ValidatorRequest.RoleValidator
{
    public class AssignPermissionsForRoleValidator : AbstractValidator<AssignPermissionsForRoleRequest>
    {
        public AssignPermissionsForRoleValidator(ISupermarketDbContext pContext)
        {
            RuleFor(x => x.RoleId)
                .MustAsync(async (roleId, token) =>
                {
                    return await pContext.Roles.FindAsync(roleId) != null;
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
