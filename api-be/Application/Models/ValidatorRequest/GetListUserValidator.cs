

using api_be.Core.Domain.Interfaces;
using api_be.Application.Models.Request;
using api_be.Application.Models.ValidatorRequest.DefaultValidatorBase;
using FluentValidation;
using api_be.Infrastructure.DB;

namespace api_be.Application.Models.ValidatorRequest
{
    public class GetListUserValidator : AbstractValidator<GetListUserRequest>
    {
        public GetListUserValidator(ISupermarketDbContext pContext)
        {
            Include(new ListBaseCommandValidator(pContext));
        }
    }
}
