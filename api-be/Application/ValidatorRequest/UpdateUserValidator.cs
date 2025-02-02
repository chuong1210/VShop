using api_be.Core.Domain.Interfaces;
using api_be.Domain.Models.Request;
using api_be.Domain.ValidatorRequest.BaseUser;
using FluentValidation;
using api_be.Infrastructure.DB;

namespace api_be.Application.ValidatorRequest
{
    public class UpdateUserValidator : AbstractValidator<UpdateUserRequest>
    {
        public UpdateUserValidator(ISupermarketDbContext pContext, int? pCurrentId = null)
        {
            Include(new BaseUserValidator(pContext, pCurrentId));

        }
    }
}
