using api_be.Domain.Interfaces;
using api_be.Entities.Auth;
using api_be.Extensions;
using api_be.Middleware;
using api_be.Models.Request.RoleRequest;
using api_be.Models.Responses;
using api_be.Models.ValidatorRequest.DefaultBase;
using api_be.Models.ValidatorRequest.RoleValidator;
using api_be.Transforms;
using api_be.ValidatorRequest.BaseCategory;
using api_be.ValidatorRequest.DefaultBase;
using Microsoft.EntityFrameworkCore;
using Sieve.Models;
using Sieve.Services;

namespace api_be.Services.Imps
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

            public Task<Result<RoleDto>> AssignPermissionsForRole(AssignPermissionsForRoleRequest request)
        {
            throw new NotImplementedException();
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


        public Task<Result<bool>> Delete(int id)
        {
            throw new NotImplementedException();
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


                //var categories = await _sieveProcessor.Apply(sieveModel, query).ToListAsync();


                var totalCount = await PaginatedResultBase.CountApplySieveAsync(_sieveProcessor, sieveModel, query);
                //var totalCount = await query.CountAsync();


                var paginatedQuery = _sieveProcessor.Apply(sieveModel, query);

                var categories = await paginatedQuery.Skip((request.Page.Value - 1) * request.PageSize.Value)
                                                .Take(request.PageSize.Value)
                                                .ToListAsync();



                var categoryDtos = _mapper.Map<List<RoleDto>>(categories);
                var paginatedResult = PaginatedResult<List<RoleDto>>.Create(categoryDtos, totalCount, request.Page.Value, request.PageSize.Value, StatusCodes.Status200OK);

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

        public Task<Result<RoleDto>> Update(CreateOrUpdateRoleRequest request)
        {
            throw new NotImplementedException();
        }
    }
}
