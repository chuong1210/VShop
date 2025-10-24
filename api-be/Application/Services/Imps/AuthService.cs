using api_be.Domain.Constants;
using api_be.Core.Entities;
using api_be.Core.Domain.Interfaces;
using api_be.Core.Entities.Auth;
using api_be.Domain.Exceptions;
using api_be.Domain.Extensions;
using api_be.Application.Models.Request;
using api_be.Application.Responses;
using api_be.Application.Models.ValidatorRequest;
using api_be.Domain.Transforms;
using AutoMapper;
using Azure.Core;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Sieve.Models;
using Sieve.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Mail;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;
using System.Threading;
using static api_be.Domain.Transforms.Modules;
using User = api_be.Core.Entities.Auth.User;
using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using api_be.Infrastructure.DB;
using api_be.Core;
using Microsoft.AspNetCore.Http;
using api_be.Domain.ResultResponses;

namespace api_be.Application.Services.Imps
{
    public class AuthService : IAuthService
    {

        private readonly ISupermarketDbContext _context;
        private readonly Microsoft.AspNetCore.Identity.IPasswordHasher<User> _passwordHasher;
        private readonly IConfiguration _configuration;
        private readonly IMapper _mapper;
        private readonly ISieveProcessor _sieveProcessor;
        private readonly IEmailService _emailService;
        private readonly IRedisTokenService _redisTokenService;
        private readonly IRedisCacheService _rediscacheService;






        public AuthService(ISupermarketDbContext pContext,
       IPasswordHasher<User> passwordHasher, IConfiguration pConfiguration, IMapper pMapper, ISieveProcessor pSieveProcessor, IEmailService emailService,IRedisTokenService redisTokenService,IRedisCacheService redisCacheService)
        {
            _context = pContext;
            _passwordHasher = passwordHasher;
            _configuration = pConfiguration;
            _mapper = pMapper;
            _sieveProcessor = pSieveProcessor;
            _redisTokenService = redisTokenService;
            _emailService = emailService;
            _rediscacheService = redisCacheService;
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
                    var userRole = new Core.Entities.Auth.UserRole
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
                string cacheKey = $"login_fail_{request.UserName}";
                int failCount = (await _rediscacheService.GetAsync<int?>(cacheKey)).GetValueOrDefault();

                if (failCount >= 5)
                {
                    return Result<LoginDto>.Failure("Bạn đã nhập sai quá nhiều lần. Hãy thử lại sau 15 phút.", StatusCodes.Status429TooManyRequests);
                }
                var validator = new LoginAccountValidator();
                var validationResult = await validator.ValidateAsync(request);

                if (validationResult.IsValid == false)
                {
                    var errorMessages = validationResult.Errors.Select(x => x.ErrorMessage).ToList();
                    return Result<LoginDto>.Failure(errorMessages, StatusCodes.Status400BadRequest);
                }

                User? user = await _context.Users.FirstOrDefaultAsync(x => x.Email == request.UserName ||
                                        x.UserName == request.UserName || x.PhoneNumber == request.UserName);


                if (user == null )
                {
                    await _rediscacheService.SetAsync(cacheKey, failCount + 1, TimeSpan.FromMinutes(15));

                    return Result<LoginDto>.Failure(IdentityTransform.UserNotExists(request.UserName), StatusCodes.Status400BadRequest);
                }
                if (user.IsEmailVerified==false)
                {
                    failCount++;
                    await _rediscacheService.SetAsync(cacheKey, failCount, TimeSpan.FromMinutes(15));
                    return Result<LoginDto>.Failure(IdentityTransform.InvalidAccount(), StatusCodes.Status400BadRequest);
                }

                var result =  _passwordHasher.VerifyHashedPassword(user, user.Password, request.Password);

                if (result != PasswordVerificationResult.Success)
                {
                    failCount++;
                    await _rediscacheService.SetAsync(cacheKey, failCount, TimeSpan.FromMinutes(15));
                    return Result<LoginDto>.Failure("Thông tin xác thực không hợp lệ!", StatusCodes.Status400BadRequest);
                }

                await _rediscacheService.RemoveAsync(cacheKey);

                JwtSecurityToken jwtSecurityToken = await JwtExtension.GenerateToken(user,_context,_configuration);
                var refreshToken = await JwtExtension.GenerateRefreshToken(user.Id,_context,_configuration);
                await _context.SaveChangesAsync();
                await _redisTokenService.CacheRefreshToken(
                       user.Id.ToString(),
                       refreshToken.Token,
                       refreshToken.ExpiryDate
                   );
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

                var verificationToken = new UserVerification
                {
                    UserId = user.Id,
                    Token = Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
                    ExpiryDate = DateTime.UtcNow.AddHours(24),
                    CreatedAt = DateTime.UtcNow,
                    IsUsed = false
                };

                await _context.UserVerifications.AddAsync(verificationToken);
                await _context.SaveChangesAsync();

                // Publish events or additional logic if needed
                var customer = new Core.Entities.Customer

                {
                    Name = request.Name,
                    Email = request.Email,
                    Phone = request.PhoneNumber,
                    Address = request.Address,
                    Gender = request.Gender
                };
                var newCustomer = await _context.Customers.AddAsync(customer);
                await _context.SaveChangesAsync(); //default(CancellationToken)
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
                        var userPermission = new Core.Entities.Auth.UserPermission
                        {
                            UserId = user.Id,
                            PermissionId = perm.Id
                        };
                        await _context.UserPermissions.AddAsync(userPermission);
                    }
                }
                await _context.SaveChangesAsync();
          
                    var role = await _context.Roles.SingleOrDefaultAsync(p => p.Name == CLAIMS_VALUES.TYPE_USER);
                    if (role != null)
                    {
                        var userRole = new Core.Entities.Auth.UserRole
                        {
                            UserId = user.Id,
                            RoleId = role.Id
                        };
                        await _context.UserRoles.AddAsync(userRole);
                    }



                //await _context.Set<UserVerification>().AddAsync(verificationToken);
                //await _context.SaveChangesAsync();

                // Generate verification link
                //var verificationLink = $"{_configuration["AppSettings:FrontendUrl"]}/auth/verify-email?token={Uri.EscapeDataString(verificationToken.Token)}";
                var verificationLink = $"{_configuration["AppSettings:FrontendUrl"]}/auth/verify-email/?token={Uri.EscapeDataString(verificationToken.Token)}";


                // Send verification email
                await _emailService.SendVerificationEmailAsync(user.Email, verificationLink);
                var fullUser = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == newUser.Entity.Id);
                try
                {
                    var userDto = _mapper.Map<UserDto>(newUser.Entity);
                    return Result<UserDto>.Success(userDto, StatusCodes.Status201Created);
                }
                catch (Exception ex)
                {
                    // In ra lỗi cụ thể và thông tin ánh xạ
                    var mappingError = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                    throw new Exception($"Mapping Error: {mappingError}");
                }

                //var userDto = new UserDto { Email = newUser.Entity.Email, PhoneNumber = newUser.Entity.PhoneNumber, Type = newUser.Entity.Type, CustomerId = newUser.Entity.CustomerId }; 
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

                var decodedToken = Uri.UnescapeDataString(request.Token);

                if (!validationResult.IsValid)
                {
                    var errorMessages = validationResult.Errors.Select(x => x.ErrorMessage).ToList();
                    return Result<bool>.Failure(errorMessages, StatusCodes.Status400BadRequest);
                }

                var verificationTokens = await _context.Set<UserVerification>()
                    .Include(t => t.User).ToListAsync();
                 var  verificationToken=   verificationTokens.FirstOrDefault(t => t.Token == decodedToken);

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
                var verificationToken = new UserVerification
                {
                    UserId = user.Id,
                    Token = Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
                    ExpiryDate = DateTime.UtcNow.AddHours(24),
                    CreatedAt = DateTime.UtcNow,
                    IsUsed = false
                };

                await _context.Set<UserVerification>().AddAsync(verificationToken);
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


        //public async Task<Result<LoginDto>> RefreshToken(BaseTokenRequest request)
        //{
        //    try
        //    {
        //        // Validate request
        //        var validator = new RefreshTokenValidator(_context,_configuration);
        //        var validationResult = await validator.ValidateAsync(request);

        //        if (!validationResult.IsValid)
        //        {
        //            var errorMessages = validationResult.Errors.Select(x => x.ErrorMessage).ToList();
        //            return Result<LoginDto>.Failure(errorMessages, StatusCodes.Status400BadRequest);
        //        }

        //        // Find the refresh token
        //        var refreshToken = await _context.Set<RefreshToken>()
        //            .Include(r => r.User)
        //            .FirstOrDefaultAsync(r => r.Token == request.RefreshToken);
        //        var user = await ValidateTokenAsync(request.AccessToken);


        //        if (refreshToken == null)
        //        {
        //            return Result<LoginDto>.Failure(IdentityTransform.InvalidRefreshToken(), StatusCodes.Status400BadRequest);
        //        }

        //        // Check if token is valid
        //        if (refreshToken.IsUsed || refreshToken.IsRevoked || refreshToken.ExpiryDate < DateTime.UtcNow)
        //        {

        //            var userTokens = await _context.RefreshTokens
        //       .Where(t => t.UserId == refreshToken.UserId)
        //       .ToListAsync();

        //            foreach (var token in userTokens)
        //            {
        //                token.IsRevoked = true;
        //            }
        //            await _context.SaveChangesAsync();

        //            return Result<LoginDto>.Failure(IdentityTransform.InvalidRefreshToken(), StatusCodes.Status400BadRequest);
        //        }

        //        // Mark current token as used
        //        refreshToken.IsUsed = true;
        //        _context.Set<RefreshToken>().Update(refreshToken);

        //        // Generate new JWT token
        //        var jwtToken = await JwtExtension.GenerateToken(refreshToken.User,_context,_configuration);

        //        // Generate new refresh token
        //        var newRefreshToken = await JwtExtension.GenerateRefreshToken(refreshToken.User.Id, _context, _configuration);
        //        await _context.SaveChangesAsync();

        //        // Create response
        //        var response = new LoginDto
        //        {
        //            Id = refreshToken.User.Id,
        //            Token = new JwtSecurityTokenHandler().WriteToken(jwtToken),
        //            RefreshToken = newRefreshToken.Token,
        //            Exp = DateTime.Now.AddMinutes(int.Parse(_configuration["JwtSettings:DurationInMinutes"]))


        //        };

        //        return Result<LoginDto>.Success(response, StatusCodes.Status200OK);
        //    }
        //    catch (Exception ex)
        //    {
        //        return Result<LoginDto>.Failure(ex.Message, StatusCodes.Status500InternalServerError);
        //    }
        //}

        public async Task<Result<LoginDto>> RefreshToken(BaseTokenRequest request)
        {
            try
            {
                // Validate request
                var validator = new RefreshTokenValidator(_context, _configuration, _redisTokenService);
                var validationResult = await validator.ValidateAsync(request);

                if (!validationResult.IsValid)
                {
                    var errorMessages = validationResult.Errors.Select(x => x.ErrorMessage).ToList();
                    return Result<LoginDto>.Failure(errorMessages, StatusCodes.Status400BadRequest);
                }

                // Validate the current token
                var user = await ValidateTokenAsync(request.AccessToken);
                if (user == null)
                {
                    return Result<LoginDto>.Failure(IdentityTransform.InvalidAccessToken(), StatusCodes.Status400BadRequest);
                }

                // Check if the current token is invalidated
                var currentToken = new JwtSecurityTokenHandler().ReadJwtToken(request.AccessToken);
                var isInvalidated = await _context.Set<InvalidatedToken>()
              .AnyAsync(t => t.JwtId == currentToken.Id && t.ExpiryTime >= DateTime.UtcNow);
                var jwtId = currentToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;


                if (await _redisTokenService.IsTokenInvalidated(jwtId) &&isInvalidated)
                {
                    return Result<LoginDto>.Failure(IdentityTransform.InvalidAccessToken(), StatusCodes.Status401Unauthorized);
                }

                // Retrieve cached refresh token
                var cachedRefreshToken = await _redisTokenService.GetCachedRefreshToken(user.Id.ToString());
                if (cachedRefreshToken != request.RefreshToken)
                {
                    return Result<LoginDto>.Failure(IdentityTransform.InvalidRefreshToken(), StatusCodes.Status400BadRequest);
                }

                var invalidatedToken = new InvalidatedToken
                {
                    JwtId = jwtId,
                    ExpiryTime = currentToken.ValidTo
                };

                _context.Set<InvalidatedToken>().Add(invalidatedToken);
                await _context.SaveChangesAsync();


                // Generate new tokens
                var jwtToken = await JwtExtension.GenerateToken(user, _context, _configuration);
                var newRefreshToken = await JwtExtension.GenerateRefreshToken(user.Id, _context, _configuration);

                // Cache new refresh token
                await _redisTokenService.CacheRefreshToken(
                    user.Id.ToString(),
                    newRefreshToken.Token,
                    newRefreshToken.ExpiryDate
                );

                // Remove old cached refresh token
                await _redisTokenService.RemoveCachedRefreshToken(user.Id.ToString());

                // Create response
                var response = new LoginDto
                {
                    Id = user.Id,
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
                    ValidateLifetime = false,
                    ValidIssuer = _configuration["JwtSettings:Issuer"],
                    ValidAudience = _configuration["JwtSettings:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                }, out var validatedToken);

                var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);  // Lấy userId từ token
                var userIdNew = principal.FindFirstValue(CONSTANT_CLAIM_TYPES.Uid);  // Lấy userId từ token

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



        public async Task<Result<bool>> Logout(BaseTokenRequest request)
        {
            try
            {
                // Validate request
                var validator = new BaseTokenValidator(_context,_redisTokenService,_configuration);
                var validationResult = await validator.ValidateAsync(request);
                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                    return Result<bool>.Failure(errors, StatusCodes.Status400BadRequest);
                }
                // Validate the token
                var user = await ValidateTokenAsync(request.AccessToken);
                if (user == null)
                {
                    return Result<bool>.Failure(IdentityTransform.InvalidRefreshToken(), StatusCodes.Status400BadRequest);
                }

                // Get the token details
                var tokenHandler = new JwtSecurityTokenHandler();
                var jwtToken = tokenHandler.ReadJwtToken(request.AccessToken);
                var jwtId = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;

                // Add token to invalidated tokens in Redis
                await _redisTokenService.AddInvalidatedToken(
                    jwtToken.Id,
                    jwtToken.ValidTo
                );

                // Remove cached refresh token
                await _redisTokenService.RemoveCachedRefreshToken(user.Id.ToString());

                var invalidatedToken = new InvalidatedToken
                {
                    JwtId = jwtId,
                    ExpiryTime = jwtToken.ValidTo
                };

                _context.Set<InvalidatedToken>().Add(invalidatedToken);
                await _context.SaveChangesAsync();

                // Optional: Remove cached refresh token
                return Result<bool>.Success(true, StatusCodes.Status200OK);
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure(ex.Message, StatusCodes.Status500InternalServerError);
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



public async Task<Result<LoginDto>> ValidateGoogleToken1(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadToken(token) as JwtSecurityToken;

            if (jsonToken == null)
            {
                return Result<LoginDto>.Failure("Invalid token", StatusCodes.Status400BadRequest);
            }

            // Verify the token's issuer
            if (jsonToken.Issuer != "accounts.google.com" && jsonToken.Issuer != "https://accounts.google.com")
            {
                return Result<LoginDto>.Failure("Invalid token issuer", StatusCodes.Status400BadRequest);
            }

            // Verify the audience (your Google Client ID)
            var audience = _configuration["Authentication:Google:ClientId"];
            if (!jsonToken.Audiences.Contains(audience))
            {
                return Result<LoginDto>.Failure("Invalid token audience", StatusCodes.Status400BadRequest);
            }

            // Verify the expiration time
            if (jsonToken.ValidTo < DateTime.UtcNow)
            {
                return Result<LoginDto>.Failure("Token has expired", StatusCodes.Status400BadRequest);
            }

            // Extract user information from the token
            var email = jsonToken.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
            var name = jsonToken.Claims.FirstOrDefault(c => c.Type == "name")?.Value;

            if (string.IsNullOrEmpty(email))
            {
                return Result<LoginDto>.Failure("Email not found in token", StatusCodes.Status400BadRequest);
            }

            // Check if user exists
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                // Create new user
                user = new User
                {
                    Email = email,
                    UserName = email,
                    //Name = name ?? email,
                    IsEmailVerified = true,
                    Type = User.UserType.User,

                    Password = _passwordHasher.HashPassword(null, Convert.ToBase64String(Guid.NewGuid().ToByteArray()))
                };

                await _context.Users.AddAsync(user);
                await _context.SaveChangesAsync();

                // Create customer profile
                var customer = new Core.Entities.Customer
                {
                    Name = name ?? email,
                    Email = email
                };

                await _context.Customers.AddAsync(customer);
                await _context.SaveChangesAsync();

                user.CustomerId = customer.Id;
                _context.Users.Update(user);
                await _context.SaveChangesAsync();
            }

            // Generate JWT token
            var jwtToken = await JwtExtension.GenerateToken(user, _context, _configuration);
            var refreshToken = await JwtExtension.GenerateRefreshToken(user.Id, _context, _configuration);

            await _context.SaveChangesAsync();

            var loginDto = new LoginDto
            {
                Id = user.Id,
                Token = new JwtSecurityTokenHandler().WriteToken(jwtToken),
                RefreshToken = refreshToken.Token,
                Exp = DateTime.Now.AddMinutes(int.Parse(_configuration["JwtSettings:DurationInMinutes"]))
            };

            return Result<LoginDto>.Success(loginDto, StatusCodes.Status200OK);
        }
        catch (Exception ex)
        {
            return Result<LoginDto>.Failure(ex.Message, StatusCodes.Status500InternalServerError);
        }
    }


    public async Task<Result<LoginDto>> ValidateGoogleToken(string token)
        {
            try
            {
                var settings = new GoogleJsonWebSignature.ValidationSettings()
                {
                    Audience = new[] { _configuration["Authentication:Google:ClientId"] }
                };

                var payload = await GoogleJsonWebSignature.ValidateAsync(token, settings);

                // Check if user exists
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == payload.Email);

                if (user == null)
                {
                    // Create new user
                    user = new User
                    {
                        Email = payload.Email,
                        UserName = payload.Email,
                        //Name = payload.Name,
                        IsEmailVerified = true,
                        Type = User.UserType.User,
                        Password = _passwordHasher.HashPassword(null, Convert.ToBase64String(Guid.NewGuid().ToByteArray()))
                    };

                    await _context.Users.AddAsync(user);
                    await _context.SaveChangesAsync();

                    // Create customer profile
                    var customer = new Core.Entities.Customer
                    {
                        Name = payload.Name,
                        Email = payload.Email
                    };

                    await _context.Customers.AddAsync(customer);
                    await _context.SaveChangesAsync();

                    user.CustomerId = customer.Id;
                    _context.Users.Update(user);
                    await _context.SaveChangesAsync();
                }

                // Generate JWT token
                var jwtToken = await JwtExtension.GenerateToken(user, _context, _configuration);
                var refreshToken = await JwtExtension.GenerateRefreshToken(user.Id, _context, _configuration);

                await _context.SaveChangesAsync();

                var loginDto = new LoginDto
                {
                    Id = user.Id,
                    Token = new JwtSecurityTokenHandler().WriteToken(jwtToken),
                    RefreshToken = refreshToken.Token,

                    Exp = DateTime.Now.AddMinutes(int.Parse(_configuration["JwtSettings:DurationInMinutes"]))

                };

                return Result<LoginDto>.Success(loginDto, StatusCodes.Status200OK);
            }
            catch (Exception ex)
            {
                return Result<LoginDto>.Failure(ex.Message, StatusCodes.Status500InternalServerError);
            }
        }



        public async Task<Result<bool>> ForgotPassword(ForgotPasswordRequest request)
        {
            try
            {
                var validator = new ForgotPasswordValidator(_context);
                var validationResult = await validator.ValidateAsync(request);

                if (!validationResult.IsValid)
                {
                    var errorMessages = validationResult.Errors.Select(x => x.ErrorMessage).ToList();
                    return Result<bool>.Failure(errorMessages, StatusCodes.Status400BadRequest);
                }

                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == request.Email);

                // Revoke any existing unused password reset tokens
                var existingTokens = await _context.UserVerifications
                    .Where(t => t.UserId == user.Id && !t.IsUsed && t.ExpiryDate > DateTime.UtcNow)
                    .ToListAsync();

                foreach (var token in existingTokens)
                {
                    token.IsUsed = true;
                }

                // Generate new password reset token
                var resetToken = new UserVerification
                {
                    UserId = user.Id,
                    Token = Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
                    ExpiryDate = DateTime.UtcNow.AddHours(1), // Token expires in 1 hour
                    CreatedAt = DateTime.UtcNow,
                    IsUsed = false
                };

                await _context.Set<UserVerification>().AddAsync(resetToken);
                await _context.SaveChangesAsync();

                // Generate reset link
                var resetLink = $"{_configuration["AppSettings:FrontendUrl"]}/reset-password?token={resetToken.Token}";

                // Send password reset email
                await _emailService.SendPasswordResetEmailAsync(user.Email, resetLink);

                return Result<bool>.Success(true, StatusCodes.Status200OK);
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure(ex.Message, StatusCodes.Status500InternalServerError);
            }
        }


        public async Task<Result<bool>> ResetPassword(ResetPasswordRequest request)
        {
            try
            {
                var validator = new ResetPasswordValidator(_context, _configuration);
                var validationResult = await validator.ValidateAsync(request);

                if (!validationResult.IsValid)
                {
                    var errorMessages = validationResult.Errors.Select(x => x.ErrorMessage).ToList();
                    return Result<bool>.Failure(errorMessages, StatusCodes.Status400BadRequest);
                }

                var resetToken = await _context.Set<UserVerification>()
                    .Include(t => t.User)
                    .FirstOrDefaultAsync(t => t.Token == request.Token);

                // Double-check token validity
                if (resetToken == null || resetToken.IsUsed || resetToken.ExpiryDate < DateTime.UtcNow)
                {
                    return Result<bool>.Failure("Token không hợp lệ hoặc đã hết hạn", StatusCodes.Status400BadRequest);
                }

                // Update password
                var user = resetToken.User;
                user.Password = _passwordHasher.HashPassword(user, request.NewPassword);

                // Mark token as used
                resetToken.IsUsed = true;

                // Revoke all refresh tokens for security
                var refreshTokens = await _context.RefreshTokens
                    .Where(t => t.UserId == user.Id && !t.IsUsed && !t.IsRevoked)
                    .ToListAsync();

                foreach (var token in refreshTokens)
                {
                    token.IsRevoked = true;
                }

                await _context.SaveChangesAsync();

                // Send confirmation email
             
                return Result<bool>.Success(true, StatusCodes.Status200OK);
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure(ex.Message, StatusCodes.Status500InternalServerError);
            }
        }
    }
}
