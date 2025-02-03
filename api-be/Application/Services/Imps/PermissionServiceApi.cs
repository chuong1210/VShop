using api_be.Core.Domain.Interfaces;
using api_be.Core.Entities.Auth;
using api_be.Domain.Extensions;
using api_be.Domain.Models.Request;
using api_be.Domain.Models.Responses;
using api_be.Application.ValidatorRequest;
using api_be.Domain.Transforms;
using Microsoft.EntityFrameworkCore;
using Sieve.Services;
using api_be.Infrastructure.DB;
using Microsoft.Extensions.Configuration;
using AutoMapper;
using Microsoft.AspNetCore.Http;

namespace api_be.Application.Services.Imps
{
    public class PermissionServiceApi:IPermissionServiceApi
    {

        private readonly ISupermarketDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IMapper _mapper;
        private readonly ISieveProcessor _sieveProcessor;
        public PermissionServiceApi(ISupermarketDbContext pContext, IConfiguration pConfiguration, IMapper pMapper, ISieveProcessor pSieveProcessor)
        {
            _context = pContext;
            _configuration = pConfiguration;
            _mapper = pMapper;
            _sieveProcessor = pSieveProcessor;
        }


        public async Task<Result<PermissionDto>> AddPermission(CreatePermissionRequest request)
        {
            try
            {
                // Validate request
                var validator = new CreatePermissionValidator();
                var validationResult = await validator.ValidateAsync(request);

                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                    return Result<PermissionDto>.Failure(errors, StatusCodes.Status400BadRequest);
                }

                // Check if permission already exists
                var existingPermission = await _context.Permissions
                                        .FirstOrDefaultAsync(p => p.Name == request.Name);

                if (existingPermission != null)
                {
                    return Result<PermissionDto>.Failure(ValidatorTransform.Exists(Modules.Permission.Module), StatusCodes.Status400BadRequest);
                }

                // Add new permission
                var permission = new Permission
                {
                    Name = request.Name
                };

                var newPermission = await _context.Permissions.AddAsync(permission);
                await _context.SaveChangesAsync();

                var permissionDto = _mapper.Map<PermissionDto>(newPermission.Entity);

                return Result<PermissionDto>.Success(permissionDto, StatusCodes.Status201Created);
            }
            catch (Exception ex)
            {
                return Result<PermissionDto>.Failure($"An error occurred: {ex.Message}", StatusCodes.Status500InternalServerError);
            }
        }

        public async Task<Result<RoleDto>> AssignPermissionsToRole(AssignPermissionsToRoleRequest request)
        {
            try
            {
                var validator = new AssignPermissionsToRoleValidator(_context);
                var validationResult = await validator.ValidateAsync(request);

                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                    return Result<RoleDto>.Failure(errors, StatusCodes.Status400BadRequest);
                }
                var role = await _context.Roles
                                    .Include(r => r.RolePermissions)
                                    .ThenInclude(rp => rp.Permission)
                                    .FirstOrDefaultAsync(r => r.Id == request.RoleId);

                if (role == null)
                {
                    return Result<RoleDto>.Failure(ValidatorTransform.NotExists(Modules.Role.Module), StatusCodes.Status404NotFound);
                }

                var permissions = await _context.Permissions
                                        .Where(p => request.Permissions.Contains(p.Name))
                                        .ToListAsync();

                if (permissions.Count != request.Permissions.Count)
                {
                    var invalidPermissions = request.Permissions.Except(permissions.Select(p => p.Name)).ToList();
                    return Result<RoleDto>.Failure($"Invalid permissions: {string.Join(", ", invalidPermissions)}", StatusCodes.Status400BadRequest);
                }

                // Add new permissions to the role
                foreach (var permission in permissions)
                {
                    if (!role.RolePermissions.Any(rp => rp.PermissionId == permission.Id))
                    {
                        var rolePermission = new RolePermission
                        {
                            RoleId = role.Id,
                            PermissionId = permission.Id
                        };
                        await _context.RolePermissions.AddAsync(rolePermission);
                    }
                }

                await _context.SaveChangesAsync();

                // Map updated role to DTO
                var roleDto = _mapper.Map<RoleDto>(role);

                return Result<RoleDto>.Success(roleDto, StatusCodes.Status200OK);
            }
            catch (Exception ex)
            {
                return Result<RoleDto>.Failure($"An error occurred: {ex.Message}", StatusCodes.Status500InternalServerError);
            }
        }

    }
}
