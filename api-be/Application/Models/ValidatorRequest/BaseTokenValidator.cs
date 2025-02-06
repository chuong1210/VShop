using api_be.Core.Domain.Interfaces;
using api_be.Core.Entities.Auth;
using api_be.Application.Models.Request;
using api_be.Application.Services;
using api_be.Domain.Transforms;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using api_be.Infrastructure.DB;
using Microsoft.Extensions.Configuration;
using api_be.Domain.Extensions;


namespace api_be.Application.Models.ValidatorRequest
{
    public class BaseTokenValidator : AbstractValidator<BaseTokenRequest>
    {
        private readonly ISupermarketDbContext _context;
        private readonly IRedisTokenService _redisTokenService;
        private readonly IConfiguration _configuration;

        public BaseTokenValidator(ISupermarketDbContext context,
                                  IRedisTokenService redisTokenService,
                                  IConfiguration configuration)
        {
            _context = context;
            _redisTokenService = redisTokenService;
            _configuration = configuration;

            // Validate access token existence and format
            RuleFor(x => x.AccessToken)
                .NotEmpty().WithMessage(IdentityTransform.TokenRequired())
                .Must(token => JwtExtension.BeAValidJwtToken(token, configuration))
                .WithMessage(IdentityTransform.InvalidAccessToken())
                .MustAsync(async (token, cancellation) => await BeTokenNotInvalidatedAsync(token))
                .WithMessage(IdentityTransform.AccessTokenInvalidated());
        }

        private async Task<bool> BeTokenNotInvalidatedAsync(string accessToken)
        {
            // Extract JwtId from the token
            var currentToken = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);
            var jwtId = currentToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;

            if (string.IsNullOrEmpty(jwtId))
            {
                return false;
            }

            // Check if the token is invalidated in Redis
            var isInvalidatedInRedis = await _redisTokenService.IsTokenInvalidated(jwtId);
            if (isInvalidatedInRedis)
            {
                return false;
            }

            // Check if the token is invalidated in the database
            var isInvalidatedInDb = await _context.Set<InvalidatedToken>()
                .AnyAsync(t => t.JwtId == jwtId && t.ExpiryTime >= DateTime.UtcNow);

            return !isInvalidatedInDb;
        }
    }
}
