using api_be.Domain.Entities;
using api_be.Domain.Interfaces;
using api_be.Exceptions;
using api_be.Extensions;
using api_be.Models.Common;
using api_be.Models.Request;
using api_be.Models.Responses;
using api_be.Models.ValidatorRequest.DefaultBase;
using api_be.Transforms;
using api_be.ValidatorRequest.BaseCategory;
using api_be.ValidatorRequest.DefaultBase;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Sieve.Models;
using Sieve.Services;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace api_be.Services.Imps
{
    public class CategoryService : ICategoryService
    {
        private readonly ISupermarketDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IMapper _mapper;
        private readonly ISieveProcessor _sieveProcessor;

        public CategoryService(ISupermarketDbContext pContext, IConfiguration pConfiguration, IMapper pMapper, ISieveProcessor pSieveProcessor)
        {
            _context = pContext;
            _configuration = pConfiguration;
            _mapper = pMapper;
            _sieveProcessor = pSieveProcessor;

        }
        public async Task<Result<CategoryDto>> Create(CreateCategoryRequest request)
        {
            try
            {
                var validator = new BaseCategoryValidator(_context);
                var validationResult = await validator.ValidateAsync(request);

                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                    return Result<CategoryDto>.Failure(errors, StatusCodes.Status400BadRequest);
                }
               Category Parent = request.ParentId != null
                 ? _context.Set<Category>().FirstOrDefault(x => x.Id == request.ParentId)
                 : null;

                var category = new Category
                {
                    Name = request.Name,
                    InternalCode = request.InternalCode,
                    Icon = request.Icon,
                    ParentId = request.ParentId,
                    Parent= Parent
                };
                var newCategory = await _context.Set<Category>().AddAsync(category);
                await _context.SaveChangesAsync();

                var categoryDto = _mapper.Map<CategoryDto>(newCategory.Entity);

                return Result<CategoryDto>.Success(categoryDto, StatusCodes.Status201Created);
            }
            catch (Exception ex)
            {
                return Result<CategoryDto>.Failure($"An error occurred: {ex.Message}", StatusCodes.Status500InternalServerError);
            }
        }

        public async Task<Result<bool>> Delete(int id)
        {
            try
            {

                if (id==null)
                {
                    return Result<bool>.Failure(ValidatorTransform.Required(Modules.Category.Module), StatusCodes.Status400BadRequest);
                    //throw new BadRequestException(string.Join(",", ex.Message));

                }

                var category = await _context.Categories.FindAsync(id);
                if (category == null)
                {
                    throw new NotFoundException(Modules.Id, id.ToString());
                }

                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();
                return Result<bool>.Success(true, StatusCodes.Status200OK);
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure($"An error occurred: {ex.Message}", StatusCodes.Status500InternalServerError);
            }
        }

        public async Task<Result<CategoryDto>> Detail(DetailBaseCommand request)
        {
            try
            {
                // Validate ID
                var validator = new DetailBaseValidator(_context);
                var validationResult = await validator.ValidateAsync(request);

                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                    return Result<CategoryDto>.Failure(errors, StatusCodes.Status400BadRequest);
                }

                //// Fetch category from database
                var category2 = await _context.Set<Category>()
                    .Include(c => c.Parent) // Include Parent category if needed
                    .FirstOrDefaultAsync(c => c.Id == request.Id);
                var category = _context.Set<Category>().FilterDeleted().Where(x => x.Id == request.Id);

                if (request.IsAllDetail)
                {
                    category = category.Include(x => x.Parent);
                }
                var findEntity = await category.SingleOrDefaultAsync();

                if (findEntity is  null)
                {
                    return Result<CategoryDto>.Failure(ValidatorTransform.NotExistsValue(Modules.Id, request.Id.ToString()),
                     StatusCodes.Status404NotFound);
                }



                // Map to DTO

                var categoryDto = _mapper.Map<CategoryDto>(findEntity);

                return Result<CategoryDto>.Success(categoryDto, StatusCodes.Status200OK);
            }
            catch (Exception ex)
            {
                return Result<CategoryDto>.Failure($"An error occurred: {ex.Message}", StatusCodes.Status500InternalServerError);
            }
        }

        public async Task<PaginatedResult<List<CategoryDto>>> GetList(ListBaseCommand request)
        {
            try
            {
                var validator = new ListBaseCommandValidator(_context);
                var validationResult = await validator.ValidateAsync(request);

                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                    return PaginatedResult<List<CategoryDto>>.Failure(StatusCodes.Status400BadRequest, errors);

                }
                 var query = _context.Set<Category>().FilterDeleted();

                //var query = _context.Categories.AsQueryable();

                if (request.IsAllDetail)
                {
                    query = query.Include(x => x.Parent);
                }


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

            

                var categoryDtos = _mapper.Map<List<CategoryDto>>(categories);
                var paginatedResult = PaginatedResult<List<CategoryDto>>.Create(categoryDtos, totalCount, request.Page.Value, request.PageSize.Value, StatusCodes.Status200OK);

                return paginatedResult;
            }
            catch (Exception ex)
            {
                return PaginatedResult<List<CategoryDto>>.Failure(StatusCodes.Status500InternalServerError, new List<string> { ex.Message });
            }
        }

        public async Task<Result<CategoryDto>> Update(UpdateCategoryRequest request)
        {
            try
            {
                var validator = new UpdateBaseValidator<UpdateCategoryRequest>(_context);
                var validationResult = await validator.ValidateAsync(request);

                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                    return Result<CategoryDto>.Failure(errors, StatusCodes.Status400BadRequest);
                }

                var category = await _context.Categories.FindAsync(request.Id);
                if (category == null)
                {
                    return Result<CategoryDto>.Failure("Category not found.", StatusCodes.Status404NotFound);
                }

                category.Name = request.Name;
                category.InternalCode = request.InternalCode;
                category.Icon = request.Icon;
                category.ParentId = request.ParentId;

                _context.Categories.Update(category);
                await _context.SaveChangesAsync();

                var categoryDto = _mapper.Map<CategoryDto>(category);

                return Result<CategoryDto>.Success(categoryDto, StatusCodes.Status200OK);
            }
            catch (Exception ex)
            {
                return Result<CategoryDto>.Failure($"An error occurred: {ex.Message}", StatusCodes.Status500InternalServerError);
            }
        }

    }
}
