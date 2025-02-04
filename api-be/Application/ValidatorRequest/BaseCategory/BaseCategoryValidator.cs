
using api_be.Core.Domain.Interfaces;
using api_be.Domain.Transforms;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using static System.Net.Mime.MediaTypeNames;
using api_be.Infrastructure.DB;

namespace  api_be.Application.ValidatorRequest.BaseCategory
{
    public class BaseCategoryValidator : AbstractValidator<IBaseCategory>
    {
        // Recursive function to check if there is a cycle in the parent-child relationship
        private async Task<bool> HasCycle(ISupermarketDbContext pContext, int currentId, int originalCategoryId)
        {
            // Check if current category is an ancestor of the original category (cycle detected)
            var parentCategory = await pContext.Categories
                .Where(x => x.Id == currentId && x.IsDeleted == false)
                .Select(x => x.ParentId)
                .FirstOrDefaultAsync();

            // If parent is null, we reached the root, so no cycle
            if (parentCategory == null)
                return false;

            // If we encounter the original category, cycle is detected
            if (parentCategory == originalCategoryId)
                return true;

            // Recurse up the parent chain
            return await HasCycle(pContext, parentCategory.Value, originalCategoryId);
        }
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



           // RuleFor(x => x.ParentId)
           //.MustAsync(async (parentId, token) =>
           //{
           //    // Check if ParentId is the same as the current category or causes a loop
           //    if (pCurrentId != null && pCurrentId == parentId)
           //    {
           //        return false;
           //    }

           //    // Check if ParentId exists
           //    if (parentId != null)
           //    {
           //        var isParentExists = await pContext.Categories
           //            .AnyAsync(x => x.Id == parentId && x.IsDeleted == false);

           //        if (!isParentExists)
           //        {
           //            return false;
           //        }

           //        // Ensure no cyclic dependency (category cannot be its own ancestor)
           //        return !await HasCycle(pContext, parentId.Value, pCurrentId.Value);
           //    }

           //    return true; // Allow null ParentId (root category)
           //}).WithMessage("A category cannot be its own ancestor.");
        }
    }
}
