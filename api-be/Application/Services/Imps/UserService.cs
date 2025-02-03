using api_be.Core.Domain.Interfaces;
using api_be.Core.Entities.Auth;
using api_be.Domain.Exceptions;
using api_be.Domain.Extensions;
using api_be.Domain.Models.Request;
using api_be.Domain.Models.Responses;
using api_be.Application.ValidatorRequest;
using api_be.Domain.Transforms;
using api_be.Validator;
using api_be.Domain.DefaultValidatorBase;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Sieve.Models;
using Sieve.Services;
using api_be.Infrastructure.DB;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;

namespace api_be.Application.Services.Imps
{
    public class UserService:IUserService
    {
        private readonly ISupermarketDbContext _context;
        private readonly Microsoft.AspNetCore.Identity.IPasswordHasher<User> _passwordHasher;
        private readonly IConfiguration _configuration;
        private readonly IMapper _mapper;
        private readonly ISieveProcessor _sieveProcessor;
        private readonly IEmailService _emailService;

        public UserService(ISupermarketDbContext pContext,
       IPasswordHasher<User> passwordHasher, IConfiguration pConfiguration, IMapper pMapper, ISieveProcessor pSieveProcessor, IEmailService emailService)
        {
            _context = pContext;
            _passwordHasher = passwordHasher;
            _configuration = pConfiguration;
            _mapper = pMapper;
            _sieveProcessor = pSieveProcessor;
            _emailService = emailService;
        }
        public async Task<PaginatedResult<List<UserDto>>> GetListUser(GetListUserRequest request)
        {
            // Khởi tạo validator
            var validator = new GetListUserValidator(_context);
            var validationResult = await validator.ValidateAsync(request);


            if (!validationResult.IsValid)
            {
                var errorMessages = validationResult.Errors.Select(x => x.ErrorMessage).ToList();
                return PaginatedResult<List<UserDto>>.Failure(StatusCodes.Status400BadRequest, errorMessages);
            }
            try
            {
                //var query = _context.Users.AsQueryable();
                var query = _context.Set<User>().FilterDeleted();

                //query = _sieveProcessor.Apply(sieve, query);

                // Apply search filter if SearchKeyword is provided
                if (!string.IsNullOrEmpty(request.SearchKeyword))
                {
                    query = query.Where(x => x.UserName.Contains(request.SearchKeyword) ||
                                              x.Email.Contains(request.SearchKeyword) ||
                                              x.PhoneNumber.Contains(request.SearchKeyword)); // Add more fields as needed
                }

                // If IsAllDetail is true, include Staff and Customer details
                if (request.IsAllDetail)
                {
                    query = query.Include(x => x.Staff)
                                 .Include(x => x.Customer);
                }

                // Apply sorting and filtering using SieveProcessor
                var sieveModel = new SieveModel
                {
                    Filters = request.Filters,
                    Sorts = request.Sorts
                };

                sieveModel = _mapper.Map<SieveModel>(request); // 2 cai


                var totalCount = await PaginatedResultBase.CountApplySieveAsync(_sieveProcessor, sieveModel, query);

                // Apply sieve for pagination
                var paginatedQuery = _sieveProcessor.Apply(sieveModel, query);

                // Get the actual list of users
                var users = await paginatedQuery.Skip((request.Page.Value - 1) * request.PageSize.Value)
                                                .Take(request.PageSize.Value)
                                                .ToListAsync();

                // Map the results to DTO
                var userDtos = _mapper.Map<List<UserDto>>(users);

                // Return the paginated result
                return PaginatedResult<List<UserDto>>.Create(userDtos, totalCount, request.Page.Value, request.PageSize.Value, StatusCodes.Status200OK);
            }
            catch (Exception ex)
            {
                return PaginatedResult<List<UserDto>>.Failure(StatusCodes.Status500InternalServerError, new List<string> { ex.Message });
            }
        }



        public async Task<Result<UserDto>> Create(CreateUserRequest request)
        {
            try
            {
                var validator = new CreateUserValidator(_context);
                var validationResult = await validator.ValidateAsync(request);
                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                    return Result<UserDto>.Failure(errors, StatusCodes.Status400BadRequest);
                }


                var user = _mapper.Map<User>(request);
                user.Id = 0;
                user.Password = _passwordHasher.HashPassword(user, request.Password);
                user.Type = User.UserType.SuperAdmin;



                var newEntity = await _context.Set<User>().AddAsync(user);
                await _context.SaveChangesAsync();

                var userDto = _mapper.Map<UserDto>(newEntity.Entity);

                return (Result<UserDto>.Success(userDto, StatusCodes.Status201Created));

            }
            catch (Exception ex)
            {
                // Trả về lỗi nếu có exception
                return Result<UserDto>.Failure($"Đã có lỗi xảy ra: {ex.Message}", StatusCodes.Status500InternalServerError);
            }

        }

        public async Task<Result<UserDto>> Update(UpdateUserRequest request)
        {
            try
            {
                var validator = new UpdateUserValidator(_context);
                var validationResult = await validator.ValidateAsync(request);
                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                    return Result<UserDto>.Failure(errors, StatusCodes.Status400BadRequest);
                }


                //user = await _context.Users
                //                         .FirstOrDefaultAsync(u => u.Email == user.Email || u.PhoneNumber == user.PhoneNumber);

                //user = await _context.Users
                //       .FirstOrDefaultAsync(u =>
                //           (request.Email != null && u.Email == request.Email) ||
                //           (request.Email == null && u.PhoneNumber == request.PhoneNumber)
                //       );
                var user = await _context.Users.FindAsync(request.Id);

                if (user == null)
                {
                    throw new BadRequestException(ValidatorTransform.NotExistsValue(Modules.User.Id,
                                    request.Id.ToString()));
                }

                user.CopyPropertiesFrom(request);


                //User user = _mapper.Map<User>(request);

                if (user.Password != null)
                {

                    user.Password = _passwordHasher.HashPassword((User)user, request.Password);

                }
                user.PhoneNumber = request.PhoneNumber;
                user.Email = request.Email;


                var newEntity = _context.Set<User>().Update(user);
                await _context.SaveChangesAsync();

                var userDto = _mapper.Map<UserDto>(newEntity.Entity);

                return (Result<UserDto>.Success(userDto, StatusCodes.Status200OK));

            }
            catch (Exception ex)
            {
                // Trả về lỗi nếu có exception
                return Result<UserDto>.Failure($"Đã có lỗi xảy ra: {ex.Message}", StatusCodes.Status500InternalServerError);
            }
        }



        public async Task<Result<bool>> Delete(int userId, int currentUserId)
        {
            // Kiểm tra xem người thực hiện hành động có phải là admin không
            var currentUser = await _context.Users.FindAsync(currentUserId);
            if (currentUser == null || currentUser.Type != User.UserType.Admin)
            {
                return Result<bool>.Failure(IdentityTransform.ForbiddenException(), StatusCodes.Status401Unauthorized);
            }

            // Kiểm tra xem người dùng có tồn tại không
            var userToDelete = await _context.Users.FindAsync(userId);
            if (userToDelete == null)
            {
                return Result<bool>.Failure(IdentityTransform.UserNotExists(userId.ToString()), StatusCodes.Status401Unauthorized);
            }



            var entity = await _context.Set<User>().FirstOrDefaultAsync(x => x.Id == userId && x.IsDeleted == false);

            if (entity == null)
                throw new NotFoundException(Modules.Id, userId.ToString());

            entity.IsDeleted = true;


            _context.Set<User>().Remove(entity);
            //_context.Set<User>().Update(entity);



            //_context.Users.Remove(userToDelete);
            await _context.SaveChangesAsync();
            var result = Result<bool>.Success(true, StatusCodes.Status200OK);
            result.Messages.Add(EventTransform.DeleteObjectSuccess(objectStr: Modules.User.Module.ToString(), userId.ToString()));

            return result;
        }

        public async Task<Result<UserDto>> Detail(int id)
        {
            var UserDetail = await _context.Users.FindAsync(id.ToString());
            if (UserDetail == null)
            {
                return Result<UserDto>.Failure(IdentityTransform.UserNotExists(id.ToString()), StatusCodes.Status401Unauthorized);
            }
            var userDto = _mapper.Map<UserDto>(UserDetail);
            return Result<UserDto>.Success(userDto, StatusCodes.Status200OK);
        }
    }
}
