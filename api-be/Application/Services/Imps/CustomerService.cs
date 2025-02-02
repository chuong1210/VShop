using api_be.Core.Domain.Interfaces;
using api_be.Domain.Exceptions;
using api_be.Domain.Models.Request.CustomerRequest;
using api_be.Domain.Models.Responses;
using api_be.Application.ValidatorRequest.CustomerValidator;
using api_be.Domain.DefaultValidatorBase;
using api_be.Domain.Transforms;
using api_be.Domain.DefaultValidatorBase;
using Microsoft.EntityFrameworkCore;
using Sieve.Services;
using api_be.Core.Entities;
using api_be.Domain.Extensions;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using static api_be.Domain.Transforms.Modules;
using api_be.Domain.Constants;
using Sieve.Models;
using Customer = api_be.Core.Entities.Customer;
using api_be.Core.Entities.Auth;
using api_be.Infrastructure.DB;
using Microsoft.AspNetCore.Http;
using AutoMapper;

namespace api_be.Application.Services.Imps
{
    public class CustomerService : ICustomerService
    {
        private readonly ISupermarketDbContext _context;
        private readonly IMapper _mapper;
        private readonly ISieveProcessor _sieveProcessor;
        private readonly ICurrentUserService _currentUserService;


        public CustomerService(ISupermarketDbContext context, IMapper mapper, ISieveProcessor sieveProcessor, ICurrentUserService currentUserService)
        {
            _context = context;
            _mapper = mapper;
            _sieveProcessor = sieveProcessor;

            _currentUserService = currentUserService;
        }
        public async Task<Result<bool>> Delete(int id)
        {
            try
            {
                var customer = await _context.Set<Customer>()
                                    .Include(r => r.User)
                                    .FirstOrDefaultAsync(r => r.Id == id);

                if (customer == null)
                {
                    return Result<bool>.Failure(ValidatorTransform.NotExists(Modules.Customer.Module), StatusCodes.Status404NotFound);
                }

                // Xóa liên kết với User
                if (customer.User != null)
                {
                    _context.Set<Core.Entities.Auth.User>().RemoveRange(customer.User);
                }

           

                // Xóa Role
                _context.Set<Customer>().Remove(customer);

                await _context.SaveChangesAsync();

                return Result<bool>.Success(true, StatusCodes.Status200OK);
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure($"An error occurred: {ex.Message}", StatusCodes.Status500InternalServerError);
            }
        }

        public async Task<Result<CustomerDto>> Detail(DetailBaseCommand request)
        {

            try
            {
                // Validate ID
                var validator = new DetailBaseValidator(_context);
                var validationResult = await validator.ValidateAsync(request);

                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                    return Result<CustomerDto>.Failure(errors, StatusCodes.Status400BadRequest);
                }


                var customer = _context.Set<Core.Entities.Customer>().FilterDeleted().Where(x => x.Id == request.Id);

            if (request.IsAllDetail)
            {
                customer = customer.Include(x => x.User);
            }
            var findEntity = await customer.SingleOrDefaultAsync();

                if (findEntity is null)
                {
                    return Result<CustomerDto>.Failure(ValidatorTransform.NotExistsValue(Modules.Id, request.Id.ToString()),
                     StatusCodes.Status404NotFound);
                }



                // Map to DTO

                var customerDto = _mapper.Map<CustomerDto>(findEntity);


                return Result<CustomerDto>.Success(customerDto, StatusCodes.Status200OK);
            }
            catch (Exception ex)
            {
                return Result<CustomerDto>.Failure($"An error occurred: {ex.Message}", StatusCodes.Status500InternalServerError);
            }
        }

        public async Task<PaginatedResult<List<CustomerDto>>> GetList(ListBaseCommand request)
        {
            try
            {
                var validator = new ListBaseCommandValidator(_context);
                var validationResult = await validator.ValidateAsync(request);

                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                    return PaginatedResult<List<CustomerDto>>.Failure(StatusCodes.Status400BadRequest, errors);

                }
                var query = _context.Set<Core.Entities.Customer>().FilterDeleted();

                if (request.IsAllDetail)
                {
                    query = query.Include(x => x.User);
                }





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

                var customers = await paginatedQuery.Skip((request.Page.Value - 1) * request.PageSize.Value)
                                                .Take(request.PageSize.Value)
                                                .ToListAsync();



                var customerDtos = _mapper.Map<List<CustomerDto>>(customers);
                var paginatedResult = PaginatedResult<List<CustomerDto>>.Create(customerDtos, totalCount, request.Page.Value, request.PageSize.Value, StatusCodes.Status200OK);

                return paginatedResult;
            }
            catch (Exception ex)
            {
                return PaginatedResult<List<CustomerDto>>.Failure(StatusCodes.Status500InternalServerError, new List<string> { ex.Message });
            }
        }

        public async Task<Result<CustomerDto>> Update(UpdateCustomerRequest request)
        {
            try
            {
                var validator = new UpdateCustomerValidator(_context, null);
                var validationResult = await validator.ValidateAsync(request);
                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                    return Result<CustomerDto>.Failure(errors, StatusCodes.Status400BadRequest);
                }



                var Customer = await _context.Set<Core.Entities.Customer>().FindAsync(request.Id);

                if (Customer == null)
                {
                    throw new BadRequestException(ValidatorTransform.NotExistsValue(Modules.Customer.Module,
                                    request.Id.ToString()));
                }

                Customer.CopyPropertiesFrom(request);




                var newEntity = _context.Set<Customer>().Update(Customer);
                await _context.SaveChangesAsync();

                var CustomerDto = _mapper.Map<CustomerDto>(newEntity.Entity);

                return (Result<CustomerDto>.Success(CustomerDto, StatusCodes.Status200OK));

            }
            catch (Exception ex)
            {
                // Trả về lỗi nếu có exception
                return Result<CustomerDto>.Failure($"Đã có lỗi xảy ra: {ex.Message}", StatusCodes.Status500InternalServerError);
            }
        }
    }
}
