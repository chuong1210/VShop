using api_be.Core.Domain.Interfaces;
using api_be.Domain.Exceptions;
using api_be.Application.Models.Request.StaffRequest;
using api_be.Application.Responses;
using api_be.Application.Models.ValidatorRequest.StaffValidator;
using Microsoft.EntityFrameworkCore;
using Sieve.Services;
using api_be.Core.Entities;
using api_be.Domain.Extensions;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using static api_be.Domain.Transforms.Modules;
using api_be.Domain.Constants;
using Sieve.Models;
using api_be.Core.Entities.Auth;
using api_be.Infrastructure.DB;
using Microsoft.AspNetCore.Http;
using AutoMapper;
using api_be.Domain.ResultResponses;
using api_be.Application.Models.ValidatorRequest.DefaultValidatorBase;

using api_be.Application.Models.Request.CouponRequest;
using api_be.Application.Models.ValidatorRequest.CouponValidator.BaseCoupon;
using api_be.Domain.Transforms;
using Staff = api_be.Core.Entities.Staff;
using static api_be.Core.Entities.Auth.User;
using Microsoft.AspNetCore.Identity;
using api_be.Middleware;
using Microsoft.Extensions.DependencyInjection;

namespace api_be.Application.Services.Imps
{
    [RegisterService(ServiceLifetime.Scoped)]

    public class StaffService : IStaffService
    {
        private readonly ISupermarketDbContext _context;
        private readonly IMapper _mapper;
        private readonly ISieveProcessor _sieveProcessor;
        private readonly ICurrentUserService _currentUserService;
        private readonly IPasswordHasher<Core.Entities.Auth.User> _passwordHasher;



        public StaffService(ISupermarketDbContext context, IMapper mapper, ISieveProcessor sieveProcessor, ICurrentUserService currentUserService,IPasswordHasher<Core.Entities.Auth.User> passwordHasher)
        {
            _context = context;
            _mapper = mapper;
            _sieveProcessor = sieveProcessor;
            _currentUserService = currentUserService;
            _passwordHasher = passwordHasher;
        }
        public async Task HandleAfterCreateStaffEvent(CreateOrUpdateStaffRequest request)
        {
            // Tạo tài khoản
            var staff = await _context.Staffs
                .Where(x => x.InternalCode == request.InternalCode)
                .FirstOrDefaultAsync();

            var user = new Core.Entities.Auth.User
            {
                UserName = request.InternalCode,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                Type = UserType.SuperAdmin,
                StaffId = staff.Id,
            };
            user.Password = _passwordHasher.HashPassword(user, request.InternalCode);

            var entity = await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            await Task.CompletedTask;
        }
        public async Task<Result<StaffDto>> Create(CreateOrUpdateStaffRequest request)
        {
            var validator = new CreateOrUpdateStaffValidator(_context,null);
            var validationResult = await validator.ValidateAsync(request);

            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return Result<StaffDto>.Failure(errors, StatusCodes.Status400BadRequest);
            }
            var staff = _mapper.Map<Core.Entities.Staff>(request);
            staff.Id = 0;

            var newStaff = await _context.Set<Core.Entities.Staff>().AddAsync(staff);

            await _context.SaveChangesAsync();

            var StaffDto = _mapper.Map<StaffDto>(newStaff.Entity);

            await HandleAfterCreateStaffEvent(request);

            return Result<StaffDto>.Success(StaffDto, StatusCodes.Status201Created);
        }

        public async Task<Result<bool>> Delete(int id)
        {
            try
            {
                var Staff = await _context.Set<Core.Entities.Staff>()
                                    .Include(r => r.User)
                                    .FirstOrDefaultAsync(r => r.Id == id);

                if (Staff == null)
                {
                    return Result<bool>.Failure(ValidatorTransform.NotExists(Modules.Staff.Module), StatusCodes.Status404NotFound);
                }

                // Xóa liên kết với User
                if (Staff.User != null)
                {
                    _context.Set<Core.Entities.Auth.User>().RemoveRange(Staff.User);
                }



                // Xóa Role
                _context.Set<Core.Entities.Staff>().Remove(Staff);

                await _context.SaveChangesAsync();

                return Result<bool>.Success(true, StatusCodes.Status200OK);
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure($"An error occurred: {ex.Message}", StatusCodes.Status500InternalServerError);
            }
        }

        public async Task<Result<StaffDto>> Detail(DetailBaseCommand request)
        {

            try
            {
                // Validate ID
                var validator = new DetailBaseValidator(_context);
                var validationResult = await validator.ValidateAsync(request);

                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                    return Result<StaffDto>.Failure(errors, StatusCodes.Status400BadRequest);
                }


                var query = _context.Set<Core.Entities.Staff>().FilterDeleted().Where(x => x.Id == request.Id);

                if (request.IsAllDetail)
                {
                    query = query.Include(x => x.Position);
                }
                var findEntity = await query.SingleOrDefaultAsync();

                if (findEntity is null)
                {
                    return Result<StaffDto>.Failure(ValidatorTransform.NotExistsValue(Modules.Id, request.Id.ToString()),
                     StatusCodes.Status404NotFound);
                }



                // Map to DTO

                var StaffDto = _mapper.Map<StaffDto>(findEntity);


                return Result<StaffDto>.Success(StaffDto, StatusCodes.Status200OK);
            }
            catch (Exception ex)
            {
                return Result<StaffDto>.Failure($"An error occurred: {ex.Message}", StatusCodes.Status500InternalServerError);
            }
        }

        public async Task<PaginatedResult<List<StaffDto>>> GetList(ListBaseCommand request)
        {
            try
            {
                var validator = new ListBaseCommandValidator(_context);
                var validationResult = await validator.ValidateAsync(request);

                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                    return PaginatedResult<List<StaffDto>>.Failure(StatusCodes.Status400BadRequest, errors);

                }
                var query = _context.Set<Core.Entities.Staff>().FilterDeleted();

                if (request.IsAllDetail)
                {
                    query = query.Include(x => x.User);
                    query = query.Include(x => x.Position);

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

                var Staffs = await paginatedQuery.Skip((request.Page.Value - 1) * request.PageSize.Value)
                                                .Take(request.PageSize.Value)
                                                .ToListAsync();



                var StaffDtos = _mapper.Map<List<StaffDto>>(Staffs);
                var paginatedResult = PaginatedResult<List<StaffDto>>.Create(StaffDtos, totalCount, request.Page.Value, request.PageSize.Value, StatusCodes.Status200OK);

                return paginatedResult;
            }
            catch (Exception ex)
            {
                return PaginatedResult<List<StaffDto>>.Failure(StatusCodes.Status500InternalServerError, new List<string> { ex.Message });
            }
        }

        public async Task<Result<StaffDto>> Update(CreateOrUpdateStaffRequest request)
        {
            try
            {
                var validator = new CreateOrUpdateStaffValidator(_context, null);
                var validationResult = await validator.ValidateAsync(request);
                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                    return Result<StaffDto>.Failure(errors, StatusCodes.Status400BadRequest);
                }



                var Staff = await _context.Set<Core.Entities.Staff>().FindAsync(request.Id);

                if (Staff == null)
                {
                    throw new BadRequestException(ValidatorTransform.NotExistsValue(Modules.Staff.Module,
                                    request.Id.ToString()));
                }

                Staff.CopyPropertiesFrom(request);




                var newEntity = _context.Set<Staff>().Update(Staff);
                await _context.SaveChangesAsync();

                var StaffDto = _mapper.Map<StaffDto>(newEntity.Entity);

                return (Result<StaffDto>.Success(StaffDto, StatusCodes.Status200OK));

            }
            catch (Exception ex)
            {
                // Trả về lỗi nếu có exception
                return Result<StaffDto>.Failure($"Đã có lỗi xảy ra: {ex.Message}", StatusCodes.Status500InternalServerError);
            }
        }

  
    }
}
