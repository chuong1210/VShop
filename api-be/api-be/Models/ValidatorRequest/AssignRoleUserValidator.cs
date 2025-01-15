using api_be.Domain.Interfaces;
using api_be.Models.Request;
using api_be.Transforms;
using FluentValidation;

namespace api_be.Models.ValidatorRequest
{
    public class AssignRoleUserValidator : AbstractValidator<AssignRoleUserRequest>

    {

        public AssignRoleUserValidator(ISupermarketDbContext pContext)
        {
            RuleFor(x => x.UserId)
                .MustAsync(async (userId, token) =>
                {
                    return await pContext.Users.FindAsync(userId) != null;
                }).WithMessage(ValidatorTransform.NotExists(Modules.User.Module));

            RuleFor(x => x.RolesId)
                .MustAsync(async (rolesId, token) =>
                {
                    if(rolesId != null)
                    {
                        foreach (var roleId in rolesId)
                        {
                            var exists = await pContext.Roles.FindAsync(roleId) == null;
                            if (exists)
                            {
                                return false;
                            }
                        }
                    }    
                    return true;
                }).WithMessage(ValidatorTransform.NotExists(Modules.Role.Module));
        }
    }
}
