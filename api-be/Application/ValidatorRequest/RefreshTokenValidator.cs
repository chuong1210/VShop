using api_be.Core.Domain.Interfaces;
using api_be.Domain.Extensions;
using api_be.Domain.Models.Request;
using api_be.Application.Services;
using api_be.Domain.Transforms;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using api_be.Infrastructure.DB;
using Microsoft.Extensions.Configuration;
using api_be.Application.Services;

namespace api_be.Application.ValidatorRequest
{
    public class RefreshTokenValidator : AbstractValidator<BaseTokenRequest>
    {
     

        public RefreshTokenValidator(ISupermarketDbContext context, IConfiguration configuration,IRedisTokenService redisTokenService)
        {


            //// Validate refresh token existence
            //RuleFor(x => x.RefreshToken)
            //    .NotEmpty().WithMessage(IdentityTransform.TokenRequired())
            //    .MustAsync(async (refreshToken, cancellation) =>
            //    {
            //        var token = await _context.RefreshTokens
            //            .FirstOrDefaultAsync(t => t.Token == refreshToken);
            //        return token != null;
            //    }).WithMessage(IdentityTransform.InvalidRefreshToken())
            //    .MustAsync(async (refreshToken, cancellation) =>
            //    {
            //        var token = await _context.RefreshTokens
            //            .FirstOrDefaultAsync(t => t.Token == refreshToken);
            //        return token != null && !token.IsUsed;
            //    }).WithMessage(IdentityTransform.RefreshTokenUsed())
            //    .MustAsync(async (refreshToken, cancellation) =>
            //    {
            //        var token = await _context.RefreshTokens
            //            .FirstOrDefaultAsync(t => t.Token == refreshToken);
            //        return token != null && !token.IsRevoked;
            //    }).WithMessage(IdentityTransform.RefreshTokenRevoked())
            //    .MustAsync(async (refreshToken, cancellation) =>
            //    {
            //        var token = await _context.RefreshTokens
            //            .FirstOrDefaultAsync(t => t.Token == refreshToken);
            //        return token != null && token.ExpiryDate > DateTime.UtcNow;
            //    }).WithMessage(IdentityTransform.RefreshTokenExpired());
            Include(new BaseTokenValidator(context, redisTokenService, configuration));

            RuleFor(x => x.AccessToken)

    .Must(token => !JwtExtension.IsAccessTokenStillValid(token)).WithMessage(IdentityTransform.AccessTokenNotExpired()); ;

        }

   
}
}
