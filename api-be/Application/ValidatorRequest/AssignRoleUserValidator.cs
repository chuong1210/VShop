using api_be.Core.Domain.Interfaces;
using api_be.Domain.Models.Request;
using api_be.Domain.Transforms;
using FluentValidation;
using api_be.Infrastructure.DB;
namespace api_be.Application.ValidatorRequest
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
