
using api_be.Domain.Interfaces;
using api_be.Transforms;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using static System.Net.Mime.MediaTypeNames;

namespace api_be.ValidatorRequest.BaseCategory
{
    public class BaseCategoryValidator : AbstractValidator<IBaseCategory>
    {
        public BaseCategoryValidator(ISupermarketDbContext pContext, int? pCurrentId = null)
        {
            RuleFor(x => x.InternalCode)
                .NotEmpty().WithMessage(ValidatorTransform.Required(Modules.InternalCode))
                .MinimumLength(Modules.InternalCodeMin)
                .WithMessage(ValidatorTransform.MinimumLength(Modules.InternalCode, Modules.InternalCodeMin))
                .MaximumLength(Modules.InternalCodeMax)
                .WithMessage(ValidatorTransform.MinimumLength(Modules.InternalCode, Modules.InternalCodeMax))
                .MustAsync(async (internalCode, token) =>
                {
                    bool exists;

                    if (pCurrentId == null)
                    {
                        exists = await pContext.Categories
                        .AnyAsync(x => x.InternalCode == internalCode &&
                                       x.IsDeleted == false);
                    }
                    else
                    {
                        exists = await pContext.Categories
                        .AnyAsync(x => x.InternalCode == internalCode &&
                                       x.Id != pCurrentId &&
                                       x.IsDeleted == false);
                    }

                    return !exists;
                }).WithMessage(ValidatorTransform.Exists(Modules.InternalCode));

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage(ValidatorTransform.Required(Modules.Name))
                .MinimumLength(Modules.NameMin)
                .WithMessage(ValidatorTransform.MinimumLength(Modules.Name, Modules.NameMin))
                .MaximumLength(Modules.NameMax)
                .WithMessage(ValidatorTransform.MinimumLength(Modules.Name, Modules.NameMax))
                .MustAsync(async (name, token) =>
                {
                    bool exists;

                    if (pCurrentId == null)
                    {
                        exists = await pContext.Categories
                        .AnyAsync(x => x.Name == name &&
                                       x.IsDeleted == false);
                    }
                    else
                    {
                        exists = await pContext.Categories
                        .AnyAsync(x => x.Name == name && x.Id != pCurrentId &&
                                       x.IsDeleted == false);
                    }
                    return !exists;
                }).WithMessage(ValidatorTransform.Exists(Modules.Name));

            RuleFor(x => x.ParentId)
                .MustAsync(async (parentId, token) =>
                {
                    if (pCurrentId != null && pCurrentId == parentId)
                    {
                        return false;
                    }    
                    return parentId == null || await pContext.Categories
                                            .AnyAsync(x => x.Id == parentId &&
                                                               x.IsDeleted == false);
                }).WithMessage(ValidatorTransform.NotExists(Modules.Category.ParentId));
        }
    }
}
