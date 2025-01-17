using api_be.Domain.Interfaces;
using api_be.Models.Request;
using api_be.Models.ValidatorRequest.DefaultBase;
using api_be.ValidatorRequest.BaseCategory;
using FluentValidation;

namespace api_be.Models.ValidatorRequest
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
