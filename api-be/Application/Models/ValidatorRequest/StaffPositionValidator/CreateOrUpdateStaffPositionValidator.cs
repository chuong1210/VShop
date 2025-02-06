using api_be.Application.Models.Request.StaffPossitionRequest;
using api_be.Application.Models.Request.StaffRequest;
using api_be.Application.Models.ValidatorRequest.StaffPositionValidator.BaseStaffPosition;
using api_be.Infrastructure.DB;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace api_be.Application.Models.ValidatorRequest.StaffPositionValidator
{
    public class CreateOrUpdateStaffPositionValidator : AbstractValidator<CreateOrUpdateStaffPositionRequest>
    {
        public CreateOrUpdateStaffPositionValidator(ISupermarketDbContext pContext)
        {
            Include(new BaseStaffPositionValidator(pContext));
        }
    }
}
