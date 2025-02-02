

using api_be.Core.Domain.Interfaces;
using api_be.Domain.Models.Request;
using api_be.Domain.DefaultValidatorBase;
using FluentValidation;
using api_be.Infrastructure.DB;

namespace api_be.Validator
{
    public class GetListUserValidator : AbstractValidator<GetListUserRequest>
    {
        public GetListUserValidator(ISupermarketDbContext pContext)
        {
            Include(new ListBaseCommandValidator(pContext));
        }
    }
}
