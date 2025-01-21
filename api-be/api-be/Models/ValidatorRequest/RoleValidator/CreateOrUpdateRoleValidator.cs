using api_be.Domain.Interfaces;
using api_be.Models.Request.RoleRequest;
using api_be.Models.ValidatorRequest.RoleValidator.BaseRole;

namespace api_be.Models.ValidatorRequest.RoleValidator
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
