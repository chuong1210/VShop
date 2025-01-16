using api_be.Constants;
using api_be.Domain.Entities;
using api_be.Domain.Interfaces;
using api_be.Entities.Auth;
using api_be.Exceptions;
using api_be.Extensions;
using api_be.Models;
using api_be.Models.Request;
using api_be.Models.Responses;
using api_be.Models.ValidatorRequest;
using api_be.Transforms;
using api_be.Validator;
using api_be.ValidatorRequest.DefaultBase;
using AutoMapper;
using Azure.Core;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Sieve.Models;
using Sieve.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;
using System.Threading;
using static api_be.Transforms.Modules;
using User = api_be.Entities.Auth.User;

namespace api_be.Services.Imps
{
    public class AuthService : IAuthService
    {

        private readonly ISupermarketDbContext _context;
        private readonly Microsoft.AspNetCore.Identity.IPasswordHasher<User> _passwordHasher;
        private readonly IConfiguration _configuration;
        private readonly IMapper _mapper;
        private readonly ISieveProcessor _sieveProcessor;
        private readonly IEmailService _emailService;




        public AuthService(ISupermarketDbContext pContext,
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

        public async Task<Result<UserDto>> AssignRole(AssignRoleUserRequest request)
        {
            try
            {
                // Validate request
                var validator = new AssignRoleUserValidator(_context);
                var validationResult = await validator.ValidateAsync(request);

                if (!validationResult.IsValid)
                {
                    var errorMessages = validationResult.Errors.Select(x => x.ErrorMessage).ToList();
                    return Result<UserDto>.Failure(errorMessages, StatusCodes.Status400BadRequest);
                }

                // Lấy danh sách role hiện tại của user
                var currentRoles = await _context.UserRoles
           .Where(x => x.UserId == request.UserId)
           .Select(x => x.RoleId)
           .Where(roleId => roleId.HasValue) // Loại bỏ giá trị null
           .Select(roleId => roleId.Value)  // Chuyển về int
           .ToListAsync();

                if (request.RolesId == null || !request.RolesId.Any())
                {
                    return Result<UserDto>.Failure("Danh sách vai trò không được để trống!", StatusCodes.Status400BadRequest);
                }

                // Tìm vai trò cần thêm và cần xóa
                var addRoles = request.RolesId.Except(currentRoles).ToList();
                var deleteRoles = currentRoles.Except(request.RolesId).ToList();
                // Xóa các vai trò không còn liên kết
                var userRolesToDelete = await _context.UserRoles
                    .Where(x => x.UserId == request.UserId && deleteRoles.Contains((int)x.RoleId))
                    .ToListAsync();

                _context.UserRoles.RemoveRange(userRolesToDelete);

                // Thêm các vai trò mới
                foreach (var roleId in addRoles)
                {
                    var userRole = new Entities.Auth.UserRole
                    {
                        UserId = request.UserId,
                        RoleId = roleId
                    };
                    await _context.UserRoles.AddAsync(userRole);
                }

                // Lưu thay đổi vào database
                await _context.SaveChangesAsync();

                // Lấy thông tin user sau khi cập nhật
                var user = await _context.Users
                    .Include(x => x.UserRoles)
                    .ThenInclude(x => x.Role)
                    .Include(x => x.Staff)
                    .Where(x => x.Id == request.UserId)
                    .SingleOrDefaultAsync();

                var userDto = _mapper.Map<UserDto>(user);

                return Result<UserDto>.Success(userDto, StatusCodes.Status200OK);
            }
            catch (Exception ex)
            {
                return Result<UserDto>.Failure(ex.Message, StatusCodes.Status500InternalServerError);
            }
        }
    

    public async Task<Result<LoginDto>> Login(LoginAccountRequest request)
        {
            try
            {
                var validator = new LoginAccountValidator();
                var validationResult = await validator.ValidateAsync(request);

                if (validationResult.IsValid == false)
                {
                    var errorMessages = validationResult.Errors.Select(x => x.ErrorMessage).ToList();
                    return Result<LoginDto>.Failure(errorMessages, StatusCodes.Status400BadRequest);
                }

                User? user = await _context.Users.FirstOrDefaultAsync(x => x.Email == request.UserName ||
                                        x.UserName == request.UserName || x.PhoneNumber == request.UserName);


                if (user == null)
                {
                    return Result<LoginDto>.Failure("Tài khoản không tồn tại!", StatusCodes.Status400BadRequest);
                }
                var result = _passwordHasher.VerifyHashedPassword(user, user.Password, request.Password);

                if (result != PasswordVerificationResult.Success)
                {
                    return Result<LoginDto>.Failure("Thông tin xác thực không hợp lệ!", StatusCodes.Status400BadRequest);
                }

                JwtSecurityToken jwtSecurityToken = await JwtExtension.GenerateToken(user,_context,_configuration);
                var refreshToken = await JwtExtension.GenerateRefreshToken(user.Id,_context,_configuration);
                await _context.SaveChangesAsync();

                LoginDto auth = new LoginDto
                {
                    Id = user.Id,
                    Exp = DateTime.Now.AddMinutes(int.Parse(_configuration["JwtSettings:DurationInMinutes"])),
                    Token = new JwtSecurityTokenHandler().WriteToken(jwtSecurityToken),
                    RefreshToken = refreshToken.Token

                };

                return Result<LoginDto>.Success(auth, StatusCodes.Status200OK);
            }
            catch (Exception ex)
            {
                return Result<LoginDto>.Failure(ex.Message, StatusCodes.Status500InternalServerError);
            }
        }

        public async Task<Result<UserDto>> Register(RegisterAccountRequest request)
        {

            try
            {
                // Validate request
                var validator = new RegisterAccountValidator(_context);
                var validationResult = await validator.ValidateAsync(request);
                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                    return Result<UserDto>.Failure(errors, StatusCodes.Status400BadRequest);
                }

                // Map request to User entity
                var user = _mapper.Map<User>(request);
                user.Password = _passwordHasher.HashPassword(user, request.Password);
                user.Type = User.UserType.User;
                user.IsEmailVerified = false; // Set email as unverified


                // Save user to database
                var newUser = await _context.Set<User>().AddAsync(user);
                await _context.SaveChangesAsync();

                var verificationToken = new EmailVerification
                {
                    UserId = user.Id,
                    Token = Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
                    ExpiryDate = DateTime.UtcNow.AddHours(24),
                    CreatedAt = DateTime.UtcNow,
                    IsUsed = false
                };

                await _context.EmailVerifications.AddAsync(verificationToken);
                await _context.SaveChangesAsync();

                // Publish events or additional logic if needed
                var customer = new Domain.Entities.Customer

                {
                    Name = request.Name,
                    Email = request.Email,
                    Phone = request.PhoneNumber,
                    Address = request.Address,
                    Gender = request.Gender
                };
                var newCustomer = await _context.Customers.AddAsync(customer);
                await _context.SaveChangesAsync(default(CancellationToken));
                //await _context.SaveChangesAsync();


                //var userUD = await _context.Users
                //   .Where(x => x.UserName .Equals( request.UserName))
                //   .FirstOrDefaultAsync();
                //user.CustomerId = newCustomer.Entity.Id;
                user.CustomerId = customer.Id;
                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                // Assign default roles
                foreach (string permission in CONSTANT_PERMESSION_DEFAULT.PERMISSIONS)
                {
                    var perm = await _context.Permissions.SingleOrDefaultAsync(p => p.Name == permission);
                    if (perm != null)
                    {
                        var userPermission = new Entities.Auth.UserPermission
                        {
                            UserId = user.Id,
                            PermissionId = perm.Id
                        };
                        await _context.UserPermissions.AddAsync(userPermission);
                    }
                }
                await _context.SaveChangesAsync();



                await _context.Set<EmailVerification>().AddAsync(verificationToken);
                await _context.SaveChangesAsync();

                // Generate verification link
                var verificationLink = $"{_configuration["AppSettings:FrontendUrl"]}/verify-email?token={verificationToken.Token}";

                // Send verification email
                await _emailService.SendVerificationEmailAsync(user.Email, verificationLink);

                // Map to DTO and return result
                var userDto = _mapper.Map<UserDto>(newUser.Entity);
                return Result<UserDto>.Success(userDto, StatusCodes.Status201Created);
            }
            catch (Exception ex)
            {
                // Trả về lỗi nếu có exception
                return Result<UserDto>.Failure($"Đã có lỗi xảy ra: {ex.Message}", StatusCodes.Status500InternalServerError);
            }

        }


        // Add method to verify email
        public async Task<Result<bool>> VerifyEmail(VerifyEmailRequest request)
        {
            try
            {
                var validator = new VerifyEmailValidator();
                var validationResult = await validator.ValidateAsync(request);

                if (!validationResult.IsValid)
                {
                    var errorMessages = validationResult.Errors.Select(x => x.ErrorMessage).ToList();
                    return Result<bool>.Failure(errorMessages, StatusCodes.Status400BadRequest);
                }

                var verificationToken = await _context.Set<EmailVerification>()
                    .Include(t => t.User)
                    .FirstOrDefaultAsync(t => t.Token == request.Token);

                if (verificationToken == null)
                {
                    return Result<bool>.Failure(IdentityTransform.InvalidAccessToken(), StatusCodes.Status400BadRequest);
                }

                if (verificationToken.IsUsed)
                {
                    return Result<bool>.Failure(IdentityTransform.RefreshTokenUsed(), StatusCodes.Status400BadRequest);
                }

                if (verificationToken.ExpiryDate < DateTime.UtcNow)
                {
                    return Result<bool>.Failure("Token đã hết hạn", StatusCodes.Status400BadRequest);
                }

                verificationToken.IsUsed = true;
                verificationToken.User.IsEmailVerified = true;

                await _context.SaveChangesAsync();

                return Result<bool>.Success(true, StatusCodes.Status200OK);
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure(ex.Message, StatusCodes.Status500InternalServerError);
            }
        }

        // Add method to resend verification email
        public async Task<Result<bool>> ResendVerificationEmail(ResendVerificationEmailRequest request)
        {
            try
            {
                var validator = new ResendVerificationEmailValidator(_context);
                var validationResult = await validator.ValidateAsync(request);

                if (!validationResult.IsValid)
                {
                    var errorMessages = validationResult.Errors.Select(x => x.ErrorMessage).ToList();
                    return Result<bool>.Failure(errorMessages, StatusCodes.Status400BadRequest);
                }

                var user = await _context.Set<User>()
                    .FirstOrDefaultAsync(u => u.Email == request.Email);

                if (user.IsEmailVerified == true)
                {
                    return Result<bool>.Failure("Email đã được xác nhận", StatusCodes.Status400BadRequest);
                }

                // Generate new verification token
                var verificationToken = new EmailVerification
                {
                    UserId = user.Id,
                    Token = Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
                    ExpiryDate = DateTime.UtcNow.AddHours(24),
                    CreatedAt = DateTime.UtcNow,
                    IsUsed = false
                };

                await _context.Set<EmailVerification>().AddAsync(verificationToken);
                await _context.SaveChangesAsync();

                // Generate verification link
                var verificationLink = $"{_configuration["AppSettings:FrontendUrl"]}/verify-email?token={verificationToken.Token}";

                // Send verification email
                await _emailService.SendVerificationEmailAsync(user.Email, verificationLink);

                return Result<bool>.Success(true, StatusCodes.Status200OK);
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure(ex.Message, StatusCodes.Status500InternalServerError);
            }
        }


        public async Task<Result<LoginDto>> RefreshToken(RefreshTokenRequest request)
        {
            try
            {
                // Validate request
                var validator = new RefreshTokenValidator(_context,_configuration);
                var validationResult = await validator.ValidateAsync(request);

                if (!validationResult.IsValid)
                {
                    var errorMessages = validationResult.Errors.Select(x => x.ErrorMessage).ToList();
                    return Result<LoginDto>.Failure(errorMessages, StatusCodes.Status400BadRequest);
                }

                // Find the refresh token
                var refreshToken = await _context.Set<RefreshToken>()
                    .Include(r => r.User)
                    .FirstOrDefaultAsync(r => r.Token == request.RefreshToken);
                var user = await ValidateTokenAsync(request.AccessToken);


                if (refreshToken == null)
                {
                    return Result<LoginDto>.Failure(IdentityTransform.InvalidRefreshToken(), StatusCodes.Status400BadRequest);
                }

                // Check if token is valid
                if (refreshToken.IsUsed || refreshToken.IsRevoked || refreshToken.ExpiryDate < DateTime.UtcNow)
                {

                    var userTokens = await _context.RefreshTokens
               .Where(t => t.UserId == refreshToken.UserId)
               .ToListAsync();

                    foreach (var token in userTokens)
                    {
                        token.IsRevoked = true;
                    }
                    await _context.SaveChangesAsync();

                    return Result<LoginDto>.Failure(IdentityTransform.InvalidRefreshToken(), StatusCodes.Status400BadRequest);
                }

                // Mark current token as used
                refreshToken.IsUsed = true;
                _context.Set<RefreshToken>().Update(refreshToken);

                // Generate new JWT token
                var jwtToken = await JwtExtension.GenerateToken(refreshToken.User,_context,_configuration);

                // Generate new refresh token
                var newRefreshToken = await JwtExtension.GenerateRefreshToken(refreshToken.User.Id, _context, _configuration);
                await _context.SaveChangesAsync();

                // Create response
                var response = new LoginDto
                {
                    Id = refreshToken.User.Id,
                    Token = new JwtSecurityTokenHandler().WriteToken(jwtToken),
                    RefreshToken = newRefreshToken.Token,
                    Exp = DateTime.Now.AddMinutes(int.Parse(_configuration["JwtSettings:DurationInMinutes"]))


                };

                return Result<LoginDto>.Success(response, StatusCodes.Status200OK);
            }
            catch (Exception ex)
            {
                return Result<LoginDto>.Failure(ex.Message, StatusCodes.Status500InternalServerError);
            }
        }
        public async Task<User> ValidateTokenAsync(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["JwtSettings:Key"]);

            try
            {
                var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidIssuer = _configuration["JwtSettings:Issuer"],
                    ValidAudience = _configuration["JwtSettings:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                }, out var validatedToken);

                var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);  // Lấy userId từ token

                if (userId == null)
                    return null;

                // Tìm và trả về người dùng từ cơ sở dữ liệu
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id.ToString() == userId);
                return user;
            }
            catch
            {
                return null;
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

                if (user.Password!=null)
                {

                    user.Password = _passwordHasher.HashPassword((User)user, request.Password);

                }
                user.PhoneNumber=request.PhoneNumber;
                user.Email=request.Email;


                var newEntity =  _context.Set<User>().Update(user);
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

     
        public async Task<Result<UserDto>> ChangePassword(ChangePasswordRequest request)
        {
            try
            {
                // Initialize the validator
                var validator = new ChangePasswordValidator(_context, _passwordHasher);

                // Validate the request
                var validationResult = await validator.ValidateAsync(request);

                if (validator == null)
                {
                    return Result<UserDto>.Failure(ValidatorTransform.ValidatorFailed(), StatusCodes.Status500InternalServerError);
                }
                if (!validationResult.IsValid)
                {
                    // Collect and return validation errors if invalid
                    var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                    return Result<UserDto>.Failure(errors, StatusCodes.Status400BadRequest);
                }

                // Retrieve the user from the database
                var user = await _context.Users.FindAsync(request.UserId);
                if (user == null)
                {
                    // If the user does not exist, return a failure
                    //return Result<UserDto>.Failure("User not found.", StatusCodes.Status404NotFound);
               
                    throw new NotFoundException(Modules.User.Module, request.UserId.ToString());

                }

                // Check if the current password is correct
                //var passwordVerificationResult = _passwordHasher.VerifyHashedPassword(user, user.Password, request.CurrentPassword);
                //if (passwordVerificationResult != PasswordVerificationResult.Success)
                //{
                //    // If the current password is incorrect, return an error
                //    return Result<UserDto>.Failure("Current password is incorrect.", StatusCodes.Status400BadRequest);
                //}

                // Hash the new password

                user.Password = _passwordHasher.HashPassword(user, request.NewPassword);

                // Save the changes to the database
                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                // Map the user to a DTO and return a success result
                var userDto = _mapper.Map<UserDto>(user);
                return Result<UserDto>.Success(userDto, StatusCodes.Status200OK);
            }
            catch (Exception ex)
            {
                // Return a generic error if any exception occurs
                return Result<UserDto>.Failure($"An error occurred: {ex.Message}", StatusCodes.Status500InternalServerError);
            }

        }

      
        public async Task<Result<bool>> Delete(int userId, int currentUserId)
        {
            // Kiểm tra xem người thực hiện hành động có phải là admin không
            var currentUser = await _context.Users.FindAsync(currentUserId);
            if (currentUser == null || currentUser.Type != User.UserType.Admin)
            {
                return Result<bool>.Failure(IdentityTransform.ForbiddenException(),StatusCodes.Status401Unauthorized);
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
        public Task<Result<LoginDto>> LoginSocial(LoginAccountRequest request)
        {
            throw new NotImplementedException();
        }


    }
}
