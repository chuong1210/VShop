

using api_be.Domain.Interfaces;
using api_be.Models.Request;
using api_be.ValidatorRequest.ListBase;
using FluentValidation;

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
