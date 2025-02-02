using api_be.Core.Domain.Interfaces;
using api_be.Core.Entities;
using api_be.Domain.Exceptions;
using api_be.Domain.Extensions;
using  api_be.Domain.Models.Request.DistributorRequest ;
using api_be.Domain.Models.Responses;
using api_be.Domain.DefaultValidatorBase;
using api_be.Application.ValidatorRequest.DistributorValidator.BaseDistributor;
using api_be.Domain.Transforms;
using api_be.Domain.ValidatorRequest.BaseCategory;
using api_be.Domain.DefaultValidatorBase;
using Microsoft.EntityFrameworkCore;
using Sieve.Models;
using Sieve.Services;
using api_be.Infrastructure.DB;
using Microsoft.Extensions.Configuration;
using AutoMapper;
using Azure.Core;
using Microsoft.AspNetCore.Http;
using api_be.Middleware;
using Microsoft.Extensions.DependencyInjection;

namespace api_be.Application.Services.Imps
{
    [RegisterService(ServiceLifetime.Scoped)]

    public class DistributorService : IDistributorService
    {
        private readonly ISupermarketDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IMapper _mapper;
        private readonly ISieveProcessor _sieveProcessor;

        public DistributorService(ISupermarketDbContext pContext, IConfiguration pConfiguration, IMapper pMapper, ISieveProcessor pSieveProcessor)
        {
            _context = pContext;
            _configuration = pConfiguration;
            _mapper = pMapper;
            _sieveProcessor = pSieveProcessor;

        }
        public async Task<Result<DistributorDto>> Create(CreateOrUpdateDistributorRequest request)
        {
            try
            {
                var validator = new BaseDistributorValidator(_context);
                var validationResult = await validator.ValidateAsync(request);

                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                    return Result<DistributorDto>.Failure(errors, StatusCodes.Status400BadRequest);
                }

                var distributor = _mapper.Map<Distributor>(request); 
                var newDistributor = await _context.Set<Distributor>().AddAsync(distributor);
                await _context.SaveChangesAsync();

                var distributorDto = _mapper.Map<DistributorDto>(newDistributor.Entity);

                return Result<DistributorDto>.Success(distributorDto, StatusCodes.Status201Created);
            }
            catch (Exception ex)
            {
                return Result<DistributorDto>.Failure($"An error occurred: {ex.Message}", StatusCodes.Status500InternalServerError);
            }
        }

        public async Task<Result<bool>> Delete(int id)
        {
            try
            {

                if (id == null || id <=0)
                {
                    return Result<bool>.Failure(ValidatorTransform.Required(Modules.Distributor.Module), StatusCodes.Status400BadRequest);
                    //throw new BadRequestException(string.Join(",", ex.Message));

                }

                var distributor = await _context.Set<Distributor>().FindAsync(id);
                if (distributor == null)
                {
                    throw new NotFoundException(Modules.Distributor.Id, id.ToString());
                }

                _context.Set<Distributor>().Remove(distributor);
                await _context.SaveChangesAsync();
                return Result<bool>.Success(true, StatusCodes.Status200OK);
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure($"An error occurred: {ex.Message}", StatusCodes.Status500InternalServerError);
            }
        }

        public async Task<Result<DistributorDto>> Detail(DetailBaseCommand request)
        {
            try
            {
                // Validate ID
                var validator = new DetailBaseValidator(_context);
                var validationResult = await validator.ValidateAsync(request);

                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                    return Result<DistributorDto>.Failure(errors, StatusCodes.Status400BadRequest);
                }


                var distributor = _context.Set<Distributor>().FilterDeleted().Where(x => x.Id == request.Id);
                var findEntity = await distributor.SingleOrDefaultAsync();

                if (findEntity is null)
                {
                    return Result<DistributorDto>.Failure(ValidatorTransform.NotExistsValue(Modules.Id, request.Id.ToString()),
                     StatusCodes.Status404NotFound);
                }




                // Map to DTO

                var distributorDto = _mapper.Map<DistributorDto>(findEntity);


                return Result<DistributorDto>.Success(distributorDto, StatusCodes.Status200OK);
            }
            catch (Exception ex)
            {
                return Result<DistributorDto>.Failure($"An error occurred: {ex.Message}", StatusCodes.Status500InternalServerError);
            }
        }

        public async Task<PaginatedResult<List<DistributorDto>>> GetList(ListBaseCommand request)
        {
            try
            {
                var validator = new ListBaseCommandValidator(_context);
                var validationResult = await validator.ValidateAsync(request);

                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                    return PaginatedResult<List<DistributorDto>>.Failure(StatusCodes.Status400BadRequest, errors);

                }
                var query = _context.Set<Distributor>().FilterDeleted();

            





               var sieveModel = _mapper.Map<SieveModel>(request);




                var totalCount = await PaginatedResultBase.CountApplySieveAsync(_sieveProcessor, sieveModel, query);


                var paginatedQuery = _sieveProcessor.Apply(sieveModel, query);

                var distributors = await paginatedQuery. ToListAsync();



                var distributorDtos = _mapper.Map<List<DistributorDto>>(distributors);
                var paginatedResult = PaginatedResult<List<DistributorDto>>.Create(distributorDtos, totalCount, request.Page.Value, request.PageSize.Value, StatusCodes.Status200OK);

                return paginatedResult;
            }
            catch (Exception ex)
            {
                return PaginatedResult<List<DistributorDto>>.Failure(StatusCodes.Status500InternalServerError, new List<string> { ex.Message });
            }
        }

        public async Task<Result<DistributorDto>> Update(CreateOrUpdateDistributorRequest request)
        {
            try
            {
                var validator = new BaseDistributorValidator(_context, null);
                var validationResult = await validator.ValidateAsync(request);
                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                    return Result<DistributorDto>.Failure(errors, StatusCodes.Status400BadRequest);
                }



                var distributor = await _context.Set<Distributor>().FindAsync(request.Id);

                if (distributor == null)
                {
                    throw new BadRequestException(ValidatorTransform.NotExistsValue(Modules.Coupon.Module,
                                    request.Id.ToString()));
                }

                distributor.CopyPropertiesFrom(request);




                var newEntity = _context.Set<Distributor>().Update(distributor);
                await _context.SaveChangesAsync();

                var distributorDto = _mapper.Map<DistributorDto>(newEntity.Entity);

                return (Result<DistributorDto>.Success(distributorDto, StatusCodes.Status200OK));

            }
            catch (Exception ex)
            {
                // Trả về lỗi nếu có exception
                return Result<DistributorDto>.Failure($"Đã có lỗi xảy ra: {ex.Message}", StatusCodes.Status500InternalServerError);
            }
        }
    }
}
