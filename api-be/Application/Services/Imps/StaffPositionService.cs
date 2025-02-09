using api_be.Core.Domain.Interfaces;
using api_be.Domain.Exceptions;
using api_be.Application.Responses;
using api_be.Application.Models.ValidatorRequest.StaffPositionValidator;
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

using api_be.Domain.Transforms;
using StaffPosition = api_be.Core.Entities.StaffPosition;
using static api_be.Core.Entities.Auth.User;
using Microsoft.AspNetCore.Identity;
using api_be.Middleware;
using Microsoft.Extensions.DependencyInjection;
using api_be.Application.Models.Request.StaffPossitionRequest;

namespace api_be.Application.Services.Imps
{
    [RegisterService(ServiceLifetime.Scoped)]

    public class StaffPositionPositionService : IStaffPositionService
    {
        private readonly ISupermarketDbContext _context;
        private readonly IMapper _mapper;
        private readonly ISieveProcessor _sieveProcessor;
        private readonly ICurrentUserService _currentUserService;
        private readonly IPasswordHasher<Core.Entities.Auth.User> _passwordHasher;



        public StaffPositionPositionService(ISupermarketDbContext context, IMapper mapper, ISieveProcessor sieveProcessor, ICurrentUserService currentUserService, IPasswordHasher<Core.Entities.Auth.User> passwordHasher)
        {
            _context = context;
            _mapper = mapper;
            _sieveProcessor = sieveProcessor;
            _currentUserService = currentUserService;
            _passwordHasher = passwordHasher;
        }
        public async Task HandleAfterCreateOrUpdateOrDeleteStaffPositionEvent(CreateOrUpdateStaffPositionRequest request)
        {
            if (request.InternalCode != null && request.Id == null)
            {
                var staffPosition = await _context.StaffPositions
                    .Where(x => x.InternalCode == request.InternalCode)
                    .FirstOrDefaultAsync();
                foreach (var role in request.Roles)
                {
                    var spRole = new StaffPositionHasRole
                    {
                        RoleId = role,
                        StaffPositionId = staffPosition.Id
                    };
                    await _context.StaffPositionHasRoles.AddAsync(spRole);
                    await _context.SaveChangesAsync();
                }
            }
            else if (request.InternalCode != null && request.Id != null)
            {
                var oldRoles = await _context.StaffPositionHasRoles
                    .Where(x => x.StaffPositionId == request.Id)
                    .Select(x => x.RoleId)
                    .ToListAsync();
                var creates = request.Roles.Except(oldRoles).ToList();
                var deletes = oldRoles.Except(request.Roles).ToList();

                foreach (var role in creates)
                {
                    var spRole = new StaffPositionHasRole
                    {
                        RoleId = role,
                        StaffPositionId = request.Id
                    };
                    await _context.StaffPositionHasRoles.AddAsync(spRole);
                    await _context.SaveChangesAsync();
                }

                foreach (var role in deletes)
                {
                    var spRole = await _context.StaffPositionHasRoles
                        .Where(x => x.StaffPositionId == request.Id &&
                                    x.RoleId == role)
                        .SingleOrDefaultAsync();
                    _context.StaffPositionHasRoles.Remove(spRole);
                    await _context.SaveChangesAsync();
                }
            }
            else
            {
                var spRoles = await _context.StaffPositionHasRoles
                    .Where(x => x.StaffPositionId == request.Id)
                    .ToListAsync();

                _context.StaffPositionHasRoles.RemoveRange(spRoles);
                await _context.SaveChangesAsync();
            }

            await Task.CompletedTask;
        }
        public async Task<Result<StaffPositionDto>> Create(CreateOrUpdateStaffPositionRequest request)
        {
            var validator = new CreateOrUpdateStaffPositionValidator(_context, null);
            var validationResult = await validator.ValidateAsync(request);

            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return Result<StaffPositionDto>.Failure(errors, StatusCodes.Status400BadRequest);
            }
            var staffPosition = _mapper.Map<Core.Entities.StaffPosition>(request);

            staffPosition.Id = 0;

            var newstaffPosition = await _context.Set<Core.Entities.StaffPosition>().AddAsync(staffPosition);
            await _context.SaveChangesAsync();

            var StaffPositionDto = _mapper.Map<StaffPositionDto>(newstaffPosition.Entity);

            await HandleAfterCreateOrUpdateOrDeleteStaffPositionEvent(request);

            return Result<StaffPositionDto>.Success(StaffPositionDto, StatusCodes.Status201Created);
        }

        public async Task<Result<bool>> Delete(int id)
        {
            try
            {
                var StaffPosition = await _context.Set<Core.Entities.StaffPosition>()
                                    .FirstOrDefaultAsync(r => r.Id == id);

                if (StaffPosition == null)
                {
                    return Result<bool>.Failure(ValidatorTransform.NotExists(Modules.Staff.Module), StatusCodes.Status404NotFound);
                }

           



                // Xóa Role
                _context.Set<Core.Entities.StaffPosition>().Remove(StaffPosition);

                await _context.SaveChangesAsync();
                var request= new CreateOrUpdateStaffPositionRequest { Id = id };
                await HandleAfterCreateOrUpdateOrDeleteStaffPositionEvent(request);

                return Result<bool>.Success(true, StatusCodes.Status200OK);
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure($"An error occurred: {ex.Message}", StatusCodes.Status500InternalServerError);
            }
        }

        public async Task<Result<StaffPositionDto>> Detail(DetailBaseCommand request)
        {

            try
            {
                // Validate ID
                var validator = new DetailBaseValidator(_context);
                var validationResult = await validator.ValidateAsync(request);

                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                    return Result<StaffPositionDto>.Failure(errors, StatusCodes.Status400BadRequest);
                }


                var StaffPosition = _context.Set<Core.Entities.StaffPosition>().FilterDeleted().Where(x => x.Id == request.Id);

              
                var findEntity = await StaffPosition.SingleOrDefaultAsync();

                if (findEntity is null)
                {
                    return Result<StaffPositionDto>.Failure(ValidatorTransform.NotExistsValue(Modules.Id, request.Id.ToString()),
                     StatusCodes.Status404NotFound);
                }



                // Map to DTO

                var StaffPositionDto = _mapper.Map<StaffPositionDto>(findEntity);


                return Result<StaffPositionDto>.Success(StaffPositionDto, StatusCodes.Status200OK);
            }
            catch (Exception ex)
            {
                return Result<StaffPositionDto>.Failure($"An error occurred: {ex.Message}", StatusCodes.Status500InternalServerError);
            }
        }

        public async Task<PaginatedResult<List<StaffPositionDto>>> GetList(ListBaseCommand request)
        {
            try
            {
                var validator = new ListBaseCommandValidator(_context);
                var validationResult = await validator.ValidateAsync(request);

                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                    return PaginatedResult<List<StaffPositionDto>>.Failure(StatusCodes.Status400BadRequest, errors);

                }
                var query = _context.Set<Core.Entities.StaffPosition>().FilterDeleted();

            



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

                var StaffPositions = await paginatedQuery.Skip((request.Page.Value - 1) * request.PageSize.Value)
                                                .Take(request.PageSize.Value)
                                                .ToListAsync();



                var StaffPositionDtos = _mapper.Map<List<StaffPositionDto>>(StaffPositions);
                var paginatedResult = PaginatedResult<List<StaffPositionDto>>.Create(StaffPositionDtos, totalCount, request.Page.Value, request.PageSize.Value, StatusCodes.Status200OK);

                return paginatedResult;
            }
            catch (Exception ex)
            {
                return PaginatedResult<List<StaffPositionDto>>.Failure(StatusCodes.Status500InternalServerError, new List<string> { ex.Message });
            }
        }

        public async Task<Result<StaffPositionDto>> Update(CreateOrUpdateStaffPositionRequest request)
        {
            try
            {
                var validator = new CreateOrUpdateStaffPositionValidator(_context, null);
                var validationResult = await validator.ValidateAsync(request);
                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                    return Result<StaffPositionDto>.Failure(errors, StatusCodes.Status400BadRequest);
                }



                var StaffPosition = await _context.Set<Core.Entities.StaffPosition>().FindAsync(request.Id);

                if (StaffPosition == null)
                {
                    throw new BadRequestException(ValidatorTransform.NotExistsValue(Modules.Staff.Module,
                                    request.Id.ToString()));
                }

                StaffPosition.CopyPropertiesFrom(request);




                var newEntity = _context.Set<StaffPosition>().Update(StaffPosition);
                await _context.SaveChangesAsync();

                var StaffPositionDto = _mapper.Map<StaffPositionDto>(newEntity.Entity);
                await HandleAfterCreateOrUpdateOrDeleteStaffPositionEvent(request);


                return (Result<StaffPositionDto>.Success(StaffPositionDto, StatusCodes.Status200OK));

            }
            catch (Exception ex)
            {
                // Trả về lỗi nếu có exception
                return Result<StaffPositionDto>.Failure($"Đã có lỗi xảy ra: {ex.Message}", StatusCodes.Status500InternalServerError);
            }
        }


    }
}
