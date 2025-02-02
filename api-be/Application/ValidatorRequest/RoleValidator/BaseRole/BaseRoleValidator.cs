using api_be.Core.Domain.Interfaces;
using api_be.Domain.Transforms;
using api_be.Infrastructure.DB;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace api_be.Application.ValidatorRequest.RoleValidator.BaseRole
{
    public class BaseRoleValidator : AbstractValidator<IBaseRole>
    {
        public BaseRoleValidator(ISupermarketDbContext pContext, int? pCurrentId = null)
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage(ValidatorTransform.Required(Modules.Name))
                .MinimumLength(Modules.NameMin)
                .WithMessage(ValidatorTransform.MinimumLength(Modules.Name, Modules.NameMin))
                .MaximumLength(Modules.NameMax)
                .WithMessage(ValidatorTransform.MaximumLength(Modules.Name, Modules.NameMax))
                .MustAsync(async (name, token) =>
                {
                    bool exists;

                    if (!pCurrentId.HasValue)
                    {
                        exists = await pContext.Roles
                                .AnyAsync(x => x.Name == name &&
                                               x.IsDeleted == false);
                    }
                    else
                    {
                        exists = await pContext.Roles
                                .AnyAsync(x => x.Name == name &&
                                               x.Id != pCurrentId &&
                                               x.IsDeleted == false);
                    }
                    return !exists;
                }).WithMessage(ValidatorTransform.Exists(Modules.Name));
        }
    }
}
