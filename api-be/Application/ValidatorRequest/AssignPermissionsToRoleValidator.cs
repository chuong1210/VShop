using FluentValidation;
using api_be.Core.Entities.Auth;
using Microsoft.EntityFrameworkCore;
using api_be.Core.Domain.Interfaces;
using api_be.Domain.Models.Request;
using api_be.Infrastructure.DB;

public class AssignPermissionsToRoleValidator : AbstractValidator<AssignPermissionsToRoleRequest>
{
    private readonly ISupermarketDbContext _context;

    public AssignPermissionsToRoleValidator(ISupermarketDbContext context)
    {
        _context = context;

        RuleFor(request => request.Permissions)
            .NotNull()
            .WithMessage("Permissions cannot be null.")
            .NotEmpty()
            .WithMessage("Permissions list cannot be empty.");

        RuleForEach(request => request.Permissions)
            .NotEmpty()
            .WithMessage("Permission name cannot be empty.")
            .MustAsync(ExistsInDatabase)
            .WithMessage(permission => $"Permission '{permission}' does not exist in the system.");
    }

    private async Task<bool> ExistsInDatabase(string permission, CancellationToken cancellationToken)
    {
        return await _context.Permissions.AnyAsync(p => p.Name == permission, cancellationToken);
    }
}
