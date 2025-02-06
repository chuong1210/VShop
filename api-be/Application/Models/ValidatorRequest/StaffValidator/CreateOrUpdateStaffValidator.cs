using api_be.Application.Models.Request.StaffRequest;
using api_be.Application.Models.ValidatorRequest.DefaultValidatorBase;
using api_be.Application.Models.ValidatorRequest.StaffValidator.BaseStaff;
using api_be.Domain.Transforms;
using api_be.Infrastructure.DB;
using Elastic.Clients.Elasticsearch.MachineLearning;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace api_be.Application.Models.ValidatorRequest.StaffValidator
{
    public class CreateOrUpdateStaffValidator:AbstractValidator<CreateOrUpdateStaffRequest>
    {
        public CreateOrUpdateStaffValidator(ISupermarketDbContext pContext,int? pCurretId)
        {
            Include(new BaseStaffValidator(pContext));

            RuleFor(x => x.InternalCode)
               .NotEmpty().WithMessage(ValidatorTransform.Required(Modules.InternalCode))
               .MaximumLength(Modules.InternalCodeMax).WithMessage(ValidatorTransform.MaximumLength(Modules.InternalCode, Modules.InternalCodeMax))
               .MustAsync(async (internalCode, token) =>
               {
                   var exists = await pContext.Staffs
                        .AnyAsync(x => x.InternalCode == internalCode &&
                                       x.IsDeleted == false);
                   return !exists;
               }).WithMessage(ValidatorTransform.Exists(Modules.InternalCode));
            if(pCurretId.HasValue)
            {
                Include(new UpdateBaseValidator<CreateOrUpdateStaffRequest>(pContext));

            }
        }
    }
}
