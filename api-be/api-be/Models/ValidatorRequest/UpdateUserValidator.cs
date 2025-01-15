using api_be.Domain.Interfaces;
using api_be.Models.Request;
using api_be.ValidatorRequest.BaseUser;
using FluentValidation;

namespace api_be.Models.ValidatorRequest
{
    public class UpdateUserValidator : AbstractValidator<UpdateUserRequest>
    {
        public UpdateUserValidator(ISupermarketDbContext pContext, int? pCurrentId = null)
        {
            Include(new BaseUserValidator(pContext, pCurrentId));

        }
    }
}
