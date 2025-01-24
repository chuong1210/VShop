using api_be.Constants;
using api_be.Domain.Interfaces;
using api_be.Entities.Auth;
using api_be.Transforms;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace api_be.Extensions
{
    public static class JwtExtension
    {

        public static bool BeAValidJwtToken(string token, IConfiguration configuration)
        {
            if (string.IsNullOrEmpty(token))
                return false;

            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(configuration["JwtSettings:Key"]);

                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = configuration["JwtSettings:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = configuration["JwtSettings:Audience"],
                    // Don't validate lifetime here as we want to allow expired tokens
                    ValidateLifetime = false,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                return validatedToken != null;
            }
            catch
            {
                return false;
            }
        }

        public static bool IsAccessTokenStillValid(string token)
        {
            if (string.IsNullOrEmpty(token))
                return false;

            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var jwtToken = tokenHandler.ReadJwtToken(token);
                var expirationTime = jwtToken.ValidTo;

                // Check if token has not expired yet
                return expirationTime > DateTime.UtcNow;
            }
            catch
            {
                return false;
            }
        }

        public static JwtSecurityToken DecodeJwtToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                return tokenHandler.ReadJwtToken(token);
            }
            catch
            {
                return null;
            }
        }

        public static ClaimsPrincipal? GetPrincipalFromExpiredToken(string token, IConfiguration configuration)
        {
            var tokenParameters = new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JwtSettings:Key"])),
                ValidateLifetime = false,
                ValidAudience = configuration["JwtSettings:Audience"],

            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, tokenParameters, out SecurityToken securityToken);

            if (securityToken is not JwtSecurityToken jwtSecurityToken || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                throw new SecurityTokenException(IdentityTransform.InvalidAccessToken());

            return principal;
        }
        public static DateTime? GetTokenExpirationTime(string token)
        {
            var jwtToken = DecodeJwtToken(token);
            return jwtToken?.ValidTo;
        }
        public static string GetBearerToken(this HttpContext httpContext)
        {
            string authorization = httpContext.Request.Headers["Authorization"].FirstOrDefault();

            if (string.IsNullOrEmpty(authorization) || !authorization.StartsWith("Bearer "))
            {
                return null;
            }

            return authorization.Substring("Bearer ".Length).Trim();
        }
        public static async Task<JwtSecurityToken> GenerateToken(
  User pUser,
            ISupermarketDbContext _context,
            IConfiguration _configuration            )
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
            // Tạo ID duy nhất cho JWT (jti)
            var jwtId = Guid.NewGuid().ToString();

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, pUser.Id.ToString()),
                new Claim(CONSTANT_CLAIM_TYPES.Uid, pUser.Id.ToString()),
                new Claim(CONSTANT_CLAIM_TYPES.Type, pUser.Type.ToString()),
                new Claim(CONSTANT_CLAIM_TYPES.Staff, pUser.StaffId.ToString()),
                new Claim(CONSTANT_CLAIM_TYPES.Customer, pUser.CustomerId.ToString()),
                new Claim(CONSTANT_CLAIM_TYPES.UserName, pUser.UserName),
               new Claim(JwtRegisteredClaimNames.Jti, jwtId) // Add JwtId claim here


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
        public static async Task<RefreshToken> GenerateRefreshToken(int userId, ISupermarketDbContext _context,
            IConfiguration _configuration)
        {
            //var refreshToken = new RefreshToken
            //{
            //    UserId = userId,
            //    Token = Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
            //    ExpiryDate = DateTime.UtcNow.AddDays(7), // Refresh token valid for 7 days
            //    CreatedAt = DateTime.UtcNow,
            //    IsUsed = false,
            //    IsRevoked = false
            //};
            var randomNumber = new byte[32];
            _ = int.TryParse(_configuration.GetSection("JwtSettings").GetSection("RefreshTokenValidityIn").Value!, out int RefreshTokenValidityIn);
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
                var refreshToken = Convert.ToBase64String(randomNumber);

                var refreshTokenEntity = new RefreshToken
                {




                    UserId = userId,
                    Token = Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
                    ExpiryDate = DateTime.UtcNow.AddDays(RefreshTokenValidityIn),
                    CreatedAt = DateTime.UtcNow,
                    IsUsed = false,
                    IsRevoked = false
                };

                //await _context.Set<RefreshToken>().AddAsync(refreshTokenEntity);
                return refreshTokenEntity;
            }
        }
    }
}
