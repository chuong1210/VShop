using api_be.Auth;
using api_be.Common.Constants;
using api_be.Common.Interfaces;
using api_be.Domain.Entities;
using api_be.Extensions;
using api_be.Models;
using api_be.Models.Request;
using api_be.Models.Responses;
using api_be.Models.Validator;
using api_be.Validator;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace api_be.Services.Imps
{
    public class AuthService:IAuthService
    {

        private readonly ISupermarketDbContext _context;
        private readonly Microsoft.AspNetCore.Identity.IPasswordHasher<User> _passwordHasher;
        private readonly IConfiguration _configuration;
        private readonly IMapper _mapper;


        public AuthService(ISupermarketDbContext pContext,
       IPasswordHasher<User> passwordHasher, IConfiguration pConfiguration, IMapper pMapper)
        {
            _context = pContext;
            _passwordHasher = passwordHasher;
            _configuration = pConfiguration;
            _mapper = pMapper;

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

                JwtSecurityToken jwtSecurityToken = await GenerateToken(user);

                LoginDto auth = new LoginDto
                {
                    Id = user.Id,
                    Exp = DateTime.Now.AddMinutes(int.Parse(_configuration["JwtSettings:DurationInMinutes"])),
                    Token = new JwtSecurityTokenHandler().WriteToken(jwtSecurityToken),
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

            // Save user to database
            var newUser = await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Publish events or additional logic if needed
            var customer = new Customer

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
                    var userPermission = new UserPermission
                    {
                        UserId = user.Id,
                        PermissionId = perm.Id
                    };
                    await _context.UserPermissions.AddAsync(userPermission);
                }
            }
            await _context.SaveChangesAsync();

            // Map to DTO and return result
            var userDto = _mapper.Map<UserDto>(newUser.Entity);
            return Result<UserDto>.Success(userDto, StatusCodes.Status201Created);
        
    }
    private async Task<JwtSecurityToken> GenerateToken(User pUser)
        {
            var roles = await _context.Roles
                    .Where(x => x.UserRoles.Any(x => x.UserId == pUser.Id))
                    .ToListAsync();
            var permissions = await _context.Permissions
                    .Where(x => x.RolePermissions.Any(x => x.Role.UserRoles.Any(x => x.UserId == pUser.Id)) ||
                                x.UserPermissions.Any(x => x.UserId == pUser.Id))
                    .ToListAsync();

            var positionId = await _context.Users
                .Where(x => x.Id == pUser.Id)
                .Select(x => x.Staff.PositionId)
                .SingleOrDefaultAsync();
            if (positionId != null)
            {
                var per = await _context.StaffPositionHasRoles
                    .Where(x => x.StaffPositionId == positionId)
                    .SelectMany(x => x.Role.RolePermissions.Select(p => p.Permission))
                    .ToListAsync();
                permissions = permissions.Union(per).Distinct().ToList();
            }

            var roleClaims = roles.Select(role => new Claim(ClaimTypes.Role, role.Name));
            var permissionClaims = permissions.Select(permission => new Claim(CONSTANT_CLAIM_TYPES.Permission, permission.Name));

            var claims = new[]
            {
                new Claim(CONSTANT_CLAIM_TYPES.Uid, pUser.Id.ToString()),
                new Claim(CONSTANT_CLAIM_TYPES.Type, pUser.Type.ToString()),
                new Claim(CONSTANT_CLAIM_TYPES.Staff, pUser.StaffId.ToString()),
                new Claim(CONSTANT_CLAIM_TYPES.Customer, pUser.CustomerId.ToString()),
                new Claim(CONSTANT_CLAIM_TYPES.UserName, pUser.UserName),
            }
            .Union(permissionClaims)
            .Union(roleClaims);

            var symmetricSecurityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtSettings:Key"]));
            var signingCredentials = new SigningCredentials(symmetricSecurityKey, SecurityAlgorithms.HmacSha256);

            var jwtSecurityToken = new JwtSecurityToken(
                issuer: _configuration["JwtSettings:Issuer"],
                audience: _configuration["JwtSettings:Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(int.Parse(_configuration["JwtSettings:DurationInMinutes"])),
                signingCredentials: signingCredentials);
            return jwtSecurityToken;
        }
    
}
}
