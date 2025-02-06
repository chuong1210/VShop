using api_be.Core.Domain.Interfaces;
using api_be.Application.Models.Request;
using api_be.Application.Models.ValidatorRequest.BaseUser;
using FluentValidation;
using api_be.Infrastructure.DB;

namespace api_be.Application.Models.ValidatorRequest
{
    public class UpdateUserValidator : AbstractValidator<UpdateUserRequest>
    {
        public UpdateUserValidator(ISupermarketDbContext pContext, int? pCurrentId = null)
        {
            Include(new BaseUserValidator(pContext, pCurrentId));

        }
    }
}
