using api_be.Core.Domain.Interfaces;
using api_be.Application.Models.Request;
using api_be.Application.Models.ValidatorRequest.DefaultValidatorBase;
using api_be.Application.Models.ValidatorRequest.BaseCategory;
using FluentValidation;
using api_be.Infrastructure.DB;

namespace api_be.Application.Models.ValidatorRequest
{
    public class UpdateCategoryValidator : AbstractValidator<UpdateCategoryRequest>
    {
        public UpdateCategoryValidator(ISupermarketDbContext pContext, int? pCurrentId)
        {
            Include(new UpdateBaseValidator<UpdateCategoryRequest>(pContext));
            Include(new BaseCategoryValidator(pContext, pCurrentId));
        }
    }
}
