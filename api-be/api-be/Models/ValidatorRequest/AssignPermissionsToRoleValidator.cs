using FluentValidation;
using api_be.Entities.Auth;
using Microsoft.EntityFrameworkCore;
using api_be.Domain.Interfaces;
using api_be.Models.Request;

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
