using api_be.Core.Domain.Interfaces;
using api_be.Domain.Models.Request.RoleRequest;
using api_be.Application.ValidatorRequest.RoleValidator.BaseRole;
using api_be.Infrastructure.DB;
using FluentValidation;

namespace api_be.Application.ValidatorRequest.RoleValidator
{
    public class CreateOrUpdateRoleValidator : AbstractValidator<CreateOrUpdateRoleRequest>
    {
        public CreateOrUpdateRoleValidator(ISupermarketDbContext pContext, int? pCurrentId)
        {
            //Include(pCurrentId.HasValue ? new BaseRoleValidator(pContext, pCurrentId.Value) : new BaseRoleValidator(pContext));

            if (!pCurrentId.HasValue)
            {
                Include(new BaseRoleValidator(pContext));

            }
            else
            {
                Include(new BaseRoleValidator(pContext,pCurrentId.Value));

            }

        }
    }
}
