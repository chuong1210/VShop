using api_be.Core.Domain.Interfaces;
using api_be.Core.Entities.Auth;
using api_be.Domain.Exceptions;
using api_be.Domain.Extensions;
using api_be.Middleware;
using api_be.Application.Models.Request;
using api_be.Application.Models.Request.RoleRequest;
using api_be.Application.Responses;
using api_be.Application.Models.ValidatorRequest;
using api_be.Application.Models.ValidatorRequest.RoleValidator;
using api_be.Domain.Transforms;
using api_be.Application.Models.ValidatorRequest.BaseCategory;
using Microsoft.EntityFrameworkCore;
using Sieve.Models;
using Sieve.Services;
using System.Threading;
using api_be.Infrastructure.DB;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using api_be.Domain.ResultResponses;
using api_be.Application.Models.ValidatorRequest.DefaultValidatorBase;

namespace api_be.Application.Services.Imps
{
    [RegisterService(ServiceLifetime.Scoped)]

    public class RoleService : IRoleService
    {
            
        private readonly ISupermarketDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IMapper _mapper;
        private readonly ISieveProcessor _sieveProcessor;
        public RoleService(ISupermarketDbContext pContext, IConfiguration pConfiguration, IMapper pMapper, ISieveProcessor pSieveProcessor)
        {
            _context = pContext;
            _configuration = pConfiguration;
            _mapper = pMapper;
            _sieveProcessor = pSieveProcessor;
        }


        public async Task HandleAfterAssignPermissionsForRole(AssignPermissionsForRoleRequest request)
        {
            if (request.DeletePermission != null)
            {
                foreach (var permission in request.DeletePermission)
                {
                    var result = await _context.RolePermissions
                                .Include(x => x.Permission)
                                .FirstOrDefaultAsync(x => x.RoleId == request.RoleId &&
                                x.Permission.Name == permission);
                    if (result != null)
                    {
                        _context.RolePermissions.Remove(result);
                    }
                }
            }

            if (request.AddPermission != null)
            {
                foreach (var permission in request.AddPermission)
                {
                    var per = await _context.Permissions.FirstOrDefaultAsync(x => x.Name == permission);
                    if (per != null)
                    {
                        await _context.RolePermissions.AddAsync(new RolePermission
                        {
                            RoleId = request.RoleId,
                            PermissionId = per.Id
                        });
                    }
                }
            }
            await _context.SaveChangesAsync();

            await Task.CompletedTask;
        }
        public async Task<Result<RoleDto>> AssignPermissionsForRole(AssignPermissionsForRoleRequest request)
        {
            try
            {
                var validator = new AssignPermissionsForRoleValidator(_context);
                var validationResult = await validator.ValidateAsync(request);

                if (validationResult.IsValid == false)
                {
                    var errorMessages = validationResult.Errors.Select(x => x.ErrorMessage).ToList();
                    return Result<RoleDto>.Failure(errorMessages, StatusCodes.Status400BadRequest);
                }

                var currentPermissions = await _context.RolePermissions
                                                .Where(x => x.RoleId == request.RoleId)
                                                .Include(x => x.Permission)
                                                .Select(x => x.Permission.Name)
                                                .ToListAsync();

                var addPermission = request.PermissionsName.Except(currentPermissions).ToList();
                var deletePermission = currentPermissions.Except(request.PermissionsName).ToList();
                request.AddPermission = addPermission;
                request.DeletePermission = deletePermission;
                //await HandleAfterAssignPermissionsForRole
                //    (request.RoleId, addPermission, deletePermission);


                await HandleAfterAssignPermissionsForRole
                  (request);

                var role = await _context.Roles
                            .Include(x => x.RolePermissions)
                            .ThenInclude(x => x.Permission)
                            .Where(x => x.Id == request.RoleId)
                            .SingleOrDefaultAsync();

                var roleDto = _mapper.Map<RoleDto>(role);

                return Result<RoleDto>.Success(roleDto, StatusCodes.Status200OK);
            }
            catch (Exception ex)
            {
                return Result<RoleDto>.Failure(ex.Message, StatusCodes.Status500InternalServerError);
            }
        }

        public async Task HandleAfterCreateRole(CreateOrUpdateRoleRequest request)
        {

            var create = request.Permissions;

            var role = await _context.Roles
                                .Where(x => x.Name == request.Name).FirstOrDefaultAsync();

            for (int i = 0; i < create.Count(); i++)
            {
                var permission = await _context.Permissions
                            .FirstOrDefaultAsync(x => x.Name == create[i]);
                var user = await _context.Users
                          .FirstOrDefaultAsync(x => x.UserName == create[i]);

                var per = new RolePermission
                {
                    RoleId = role.Id,
                    PermissionId = permission.Id
                };
                await _context.RolePermissions.AddAsync(per);
            }
            await _context.SaveChangesAsync();

            await Task.CompletedTask;
        }
        public async Task<Result<RoleDto>> Create(CreateOrUpdateRoleRequest request)
        {
            try
            {
                var validator = new CreateOrUpdateRoleValidator(_context,null);
                var validationResult = await validator.ValidateAsync(request);

                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                    return Result<RoleDto>.Failure(errors, StatusCodes.Status400BadRequest);
                }
             
               var roleEntitiy = _mapper.Map<Role>(request);
                var newRole = await _context.Set<Role>().AddAsync(roleEntitiy);
                await _context.SaveChangesAsync();

                var roleDto = _mapper.Map<RoleDto>(newRole.Entity);
                var addRolePermissions=  HandleAfterCreateRole(request);
                return Result<RoleDto>.Success(roleDto, StatusCodes.Status201Created);
            }
            catch (Exception ex)
            {
                return Result<RoleDto>.Failure($"An error occurred: {ex.Message}", StatusCodes.Status500InternalServerError);
            }
        }


        public async Task<Result<bool>> Delete(int id)
        {
            try
            {
                var role = await _context.Roles
                                    .Include(r => r.RolePermissions)
                                    .Include(r => r.UserRoles)
                                    .FirstOrDefaultAsync(r => r.Id == id);

                if (role == null)
                {
                    return Result<bool>.Failure(ValidatorTransform.NotExists(Modules.Role.Module), StatusCodes.Status404NotFound);
                }

                // Xóa liên kết với RolePermissions
                if (role.RolePermissions != null)
                {
                    _context.Set<RolePermission>().RemoveRange(role.RolePermissions);
                }

                // Xóa liên kết với UserRoles
                if (role.UserRoles != null)
                {
                    _context.Set<UserRole>().RemoveRange(role.UserRoles);
                }

                // Xóa Role
                _context.Roles.Remove(role);

                await _context.SaveChangesAsync();

                return Result<bool>.Success(true, StatusCodes.Status200OK);
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure($"An error occurred: {ex.Message}", StatusCodes.Status500InternalServerError);
            }
        }

        public async Task<Result<RoleDto>> Detail(DetailBaseCommand request)
        {
            try
            {
                // Validate ID
                var validator = new DetailBaseValidator(_context);
                var validationResult = await validator.ValidateAsync(request);

                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                    return Result<RoleDto>.Failure(errors, StatusCodes.Status400BadRequest);
                }


                var category = _context.Set<Role>().FilterDeleted().Where(x => x.Id == request.Id);

                var findEntity = await category.SingleOrDefaultAsync();

                if (findEntity is null)
                {
                    return Result<RoleDto>.Failure(ValidatorTransform.NotExistsValue(Modules.Id, request.Id.ToString()),
                     StatusCodes.Status404NotFound);
                }



                // Map to DTO

                var roleDto = _mapper.Map<RoleDto>(findEntity);
                roleDto.Permissions = await _context.RolePermissions
                   .Include(x => x.Permission)
                   .Where(x => x.RoleId == roleDto.Id)
                   .Select(x => x.Permission.Name)
                   .ToListAsync();

                return Result<RoleDto>.Success(roleDto, StatusCodes.Status200OK);
            }
            catch (Exception ex)
            {
                return Result<RoleDto>.Failure($"An error occurred: {ex.Message}", StatusCodes.Status500InternalServerError);
            }
        }

        public  async Task<PaginatedResult<List<RoleDto>>> GetList(ListBaseCommand request)
        {
            try
            {
                var validator = new ListBaseCommandValidator(_context);
                var validationResult = await validator.ValidateAsync(request);

                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                    return PaginatedResult<List<RoleDto>>.Failure(StatusCodes.Status400BadRequest, errors);

                }
                var query = _context.Set<Role>().FilterDeleted();


          


                // Apply Sieve
                var sieveModel = new SieveModel
                {
                    Page = request.Page,
                    PageSize = request.PageSize,
                    Filters = request.Filters
                };

                sieveModel = _mapper.Map<SieveModel>(request);




                var totalCount = await PaginatedResultBase.CountApplySieveAsync(_sieveProcessor, sieveModel, query);


                var paginatedQuery = _sieveProcessor.Apply(sieveModel, query);

                var roles = await paginatedQuery.Skip((request.Page.Value - 1) * request.PageSize.Value)
                                                .Take(request.PageSize.Value)
                                                .ToListAsync();



                var roleDtos = _mapper.Map<List<RoleDto>>(roles);
                var paginatedResult = PaginatedResult<List<RoleDto>>.Create(roleDtos, totalCount, request.Page.Value, request.PageSize.Value, StatusCodes.Status200OK);

                return paginatedResult;
            }
            catch (Exception ex)
            {
                return PaginatedResult<List<RoleDto>>.Failure(StatusCodes.Status500InternalServerError, new List<string> { ex.Message });
            }
        }

        public async Task<PaginatedResult<List<RoleDto>>> GetListRoleWithPermission(ListBaseCommand request)
        {
            try
            {
                var validator = new ListBaseCommandValidator(_context);
                var validationResult = await validator.ValidateAsync(request);

                if (!validationResult.IsValid)
                {
                    var errorMessages = validationResult.Errors.Select(x => x.ErrorMessage).ToList();
                    return PaginatedResult<List<RoleDto>>.Failure(StatusCodes.Status400BadRequest, errorMessages);
                }
                var query = _context.Permissions.Select(x => x.Name).AsQueryable();

                request.PageSize = 1000;
                var sieve = _mapper.Map<SieveModel>(request);

                int totalCount = await PaginatedResultBase.CountApplySieveAsync(_sieveProcessor, sieve, query);

                query = _sieveProcessor.Apply(sieve, query);

                var results = await query.ToListAsync();

                var groupedPermissions = results
                    .GroupBy(p => p.Split('.')[0])
                    .Select(g => new RoleDto
                    {
                        Name = g.Key,
                        Permissions = g.Select(p => p.Split('.')[1]).ToList()
                    })
                    .ToList();

                // Phân trang
                var mapResults = _mapper.Map<List<RoleDto>>(groupedPermissions);

                return PaginatedResult<List<RoleDto>>.Success(mapResults, totalCount, request.Page, request.PageSize);
            }
            catch (Exception ex)
            {
                return PaginatedResult<List<RoleDto>>.Failure(StatusCodes.Status500InternalServerError,
                    new List<string> { ex.Message });
            }
        }

        private async Task HandleAfterUpdateRole(CreateOrUpdateRoleRequest request)
        {
            var oldPermissions = await _context.Set<RolePermission>()
                      .Include(x => x.Permission)
                      .Where(x => x.Role.Name == request.Name)
                      .Select(x => x.Permission.Name)
                      .ToListAsync();

            var newPermissions = request.Permissions;

            var create = newPermissions.Except(oldPermissions).ToList();
            var delete = oldPermissions.Except(newPermissions).ToList();

            for (int i = 0; i < create.Count(); i++)
            {
                var permission = await _context.Permissions
                            .FirstOrDefaultAsync(x => x.Name == create[i]);

                var per = new RolePermission
                {
                    RoleId = (int)request.Id,
                    PermissionId = permission.Id
                };
                await _context.RolePermissions.AddAsync(per);
            }
            await _context.SaveChangesAsync();

            for (int i = 0; i < delete.Count(); i++)
            {
                var del = await _context.RolePermissions
                            .Include(x => x.Permission)
                            .Where(x => x.RoleId == request.Id &&
                                        x.Permission.Name == delete[i])
                                        .FirstOrDefaultAsync();
                _context.Set<RolePermission>().Remove(del);
            }
            await _context.SaveChangesAsync();

            await Task.CompletedTask;
        }
        public async Task<Result<RoleDto>> Update(CreateOrUpdateRoleRequest request)
        {

            try
            {
                var validator = new CreateOrUpdateRoleValidator(_context,null);
                var validationResult = await validator.ValidateAsync(request);
                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                    return Result<RoleDto>.Failure(errors, StatusCodes.Status400BadRequest);
                }


       
                var role = await _context.Roles.FindAsync(request.Id);

                if (role == null)
                {
                    throw new BadRequestException(ValidatorTransform.NotExistsValue(Modules.Role.Module,
                                    request.Id.ToString()));
                }

              role.CopyPropertiesFrom(request);


                //User user = _mapper.Map<User>(request);

       
                var newEntity = _context.Set<Role>().Update(role);
                await _context.SaveChangesAsync();

                var roleDto = _mapper.Map<RoleDto>(newEntity.Entity);
                await HandleAfterUpdateRole(request);

                return (Result<RoleDto>.Success(roleDto, StatusCodes.Status200OK));

            }
            catch (Exception ex)
            {
                // Trả về lỗi nếu có exception
                return Result<RoleDto>.Failure($"Đã có lỗi xảy ra: {ex.Message}", StatusCodes.Status500InternalServerError);
            }
        }
    }
}
