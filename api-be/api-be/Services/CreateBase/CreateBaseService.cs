using api_be.Domain.Common;
using api_be.Domain.Interfaces;
using api_be.Models.Responses;
using api_be.Transforms;
using AutoMapper;
using FluentValidation;

namespace api_be.Services.CreateBase
{

    public abstract class CreateBaseService<TValidator, TRequest, Dto, TEntity>
            where TValidator : AbstractValidator<TRequest>
            where TRequest : class
            where TEntity : AuditableEntity
        {
            protected readonly ISupermarketDbContext _context;
            protected readonly IMapper _mapper;
            protected readonly ICurrentUserService _currentUserService;

            public CreateBaseService(ISupermarketDbContext context,
                IMapper mapper, ICurrentUserService currentUserService)
            {
                _context = context;
                _mapper = mapper;
                _currentUserService = currentUserService;
            }

            public virtual async Task<Result<Dto>> ExecuteAsync(TRequest request, CancellationToken cancellationToken)
            {
                try
                {
                    var validator = await ValidateAsync(request);

                    if (!validator.Succeeded)
                    {
                        return validator;
                    }

                    var entity = await BeforeAsync(request);
                    entity.Id = 0;

                    var createResult = await CreateAsync(entity, cancellationToken);

                    await AfterAsync(request, entity, createResult.result.Data);

                    return createResult.result;
                }
                catch (Exception ex)
                {
                    return Result<Dto>.Failure(ex.Message, StatusCodes.Status500InternalServerError);
                }
            }

            protected virtual async Task<Result<Dto>> ValidateAsync(TRequest request)
            {
                var validator = Activator.CreateInstance(typeof(TValidator), _context) as TValidator;

                if (validator == null)
                {
                    return Result<Dto>.Failure(ValidatorTransform.ValidatorFailed(), StatusCodes.Status500InternalServerError);
                }

                var validationResult = await validator.ValidateAsync(request);

                if (!validationResult.IsValid)
                {
                    var errorMessages = validationResult.Errors.Select(x => x.ErrorMessage).ToList();
                    return Result<Dto>.Failure(errorMessages, StatusCodes.Status400BadRequest);
                }

                return Result<Dto>.Success();
            }

            protected virtual async Task<TEntity> BeforeAsync(TRequest request)
            {
                return _mapper.Map<TEntity>(request);
            }

            protected virtual async Task<(Result<Dto> result, TEntity entity)> CreateAsync(TEntity entity, CancellationToken cancellationToken)
            {
                var newEntity = await _context.Set<TEntity>().AddAsync(entity, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);

                var dto = _mapper.Map<Dto>(newEntity.Entity);

                return (Result<Dto>.Success(dto, StatusCodes.Status201Created), newEntity.Entity);
            }

            protected virtual Task AfterAsync(TRequest request, TEntity entity, Dto dto)
            {
                // Override logic if needed
                return Task.CompletedTask;
            }
        }

}
