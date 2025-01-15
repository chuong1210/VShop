
using api_be.Domain.Interfaces;
using api_be.Models.Responses;
using AutoMapper;
using Azure.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Sieve.Models;
using Sieve.Services;
using api_be.Extensions;
using api_be.Domain.Common;
using api_be.Transforms;
using FluentValidation;


namespace api_be.ValidatorRequest.ListBase
{
    public record ListBaseCommand
    {
        public string? Filters { get; set; }
        public string? Sorts { get; set; }
        public int? Page { get; set; } = 1;
        public int? PageSize { get; set; } = 100;
        public bool IsAllDetail { get; set; } = false;
        public string? SearchKeyword { get; set; } // Tìm kiếm theo tên, email hoặc số điện thoại

    }


    public abstract class ListBaseCommandHandler<TValidator, TRequest, Dto, TEntity> 
        where TValidator : AbstractValidator<TRequest>
        where TRequest : ListBaseCommand
        where TEntity : AuditableEntity
    {
        protected readonly ISupermarketDbContext _context;
        protected readonly IMapper _mapper;
        protected readonly ISieveProcessor _sieveProcessor;
        protected readonly ICurrentUserService _currentUserService;

        public ListBaseCommandHandler(ISupermarketDbContext pContext,
            IMapper pMapper, ISieveProcessor pSieveProcessor,
            ICurrentUserService currentUserService)
        {
            _context = pContext;
            _mapper = pMapper;
            _sieveProcessor = pSieveProcessor;
            _currentUserService = currentUserService;
        }

        public virtual async Task<PaginatedResult<List<Dto>>> Handle(TRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var validator = await this.Validator(request);

                if (validator.Succeeded == false)
                {
                    return validator;
                }

                var list = await this.List(request, cancellationToken);

                return list;
            }
            catch (Exception ex)
            {
                return PaginatedResult<List<Dto>>
                    .Failure(StatusCodes.Status500InternalServerError,
                        new List<string> { ex.Message });
            }
        }

        protected virtual async Task<PaginatedResult<List<Dto>>> Validator(TRequest request)
        {
            var validator = Activator.CreateInstance(typeof(TValidator), _context) as TValidator;

            if (validator == null)
            {
                return PaginatedResult<List<Dto>>
                    .Failure(StatusCodes.Status500InternalServerError, new List<string> { ValidatorTransform.ValidatorFailed() });
            }

            var validationResult = await validator.ValidateAsync(request);

            if (validationResult.IsValid == false)
            {
                var errorMessages = validationResult.Errors.Select(x => x.ErrorMessage).ToList();
                return PaginatedResult<List<Dto>>
                    .Failure(StatusCodes.Status400BadRequest, errorMessages);
            }

            return PaginatedResult<List<Dto>>.Success(new List<Dto>(), 0);
        }

        protected virtual async Task<PaginatedResult<List<Dto>>> List(TRequest request, CancellationToken cancellationToken)
        {
            var query = _context.Set<TEntity>().FilterDeleted();

            var sieve = _mapper.Map<SieveModel>(request);

            query = ApplyQuery(request, query);

            int totalCount = await PaginatedResultBase.CountApplySieveAsync(_sieveProcessor, sieve, query);

            query = _sieveProcessor.Apply(sieve, query);

            var results = await query.ToListAsync();

            var mapResults = _mapper.Map<List<Dto>>(results);

            var resultListDto = await HandlerDtoAfterQuery(request, mapResults);

            return PaginatedResult<List<Dto>>.Success(resultListDto, totalCount, request.Page, request.PageSize);
        }

        protected virtual IQueryable<TEntity> ApplyQuery(TRequest request, IQueryable<TEntity> query) { return query; }

        protected virtual async Task<List<Dto>> HandlerDtoAfterQuery(TRequest request, List<Dto> listDto)
        {
            return listDto;
        }
    }
    //public abstract class ListBaseService<TEntity, TDto>          where TEntity : AuditableEntity

    //{
    //    protected readonly ISupermarketDbContext _context;
    //    protected readonly IMapper _mapper;
    //    protected readonly ISieveProcessor _sieveProcessor;

    //    protected readonly ICurrentUserService _currentUserService;

    //    protected ListBaseService(ISupermarketDbContext context, IMapper mapper, ISieveProcessor sieveProcessor, ICurrentUserService currentUserService)
    //    {
    //        _context = context;
    //        _mapper = mapper;
    //        _sieveProcessor = sieveProcessor;
    //        _currentUserService = currentUserService;
    //    }



    //    protected virtual async Task<PaginatedResult<List<TDto>>> ListAsync<TRequest>(
    //        TRequest request,
    //        Func<IQueryable<TEntity>, IQueryable<TEntity>>? applyQuery = null,
    //        Func<TRequest, List<TDto>, Task<List<TDto>>>? handlerDtoAfterQuery = null,
    //        CancellationToken cancellationToken = default
    //    ) where TRequest : ListBaseCommand
    //    {
    //        // Bắt đầu query từ DbSet
    //        var query = _context.Set<TEntity>().FilterDeleted();

    //        // Áp dụng logic query tùy chỉnh (nếu có)
    //        if (applyQuery != null)
    //        {
    //            query = applyQuery(query);
    //        }

    //        // Áp dụng SieveModel
    //        var sieve = _mapper.Map<SieveModel>(request);
    //        int totalCount = await PaginatedResultBase.CountApplySieveAsync(_sieveProcessor, sieve, query);

    //        query = _sieveProcessor.Apply(sieve, query);

    //        // Thực hiện query
    //        var results = await query.ToListAsync(cancellationToken);

    //        // Map kết quả
    //        var mapResults = _mapper.Map<List<TDto>>(results);

    //        // Xử lý DTO sau khi query (nếu cần)
    //        if (handlerDtoAfterQuery != null)
    //        {
    //            mapResults = await handlerDtoAfterQuery(request, mapResults);
    //        }

    //        return PaginatedResult<List<TDto>>.Success(mapResults, totalCount, request.Page, request.PageSize);
    //    }
    //}



}
