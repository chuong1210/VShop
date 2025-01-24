using api_be.Domain.Interfaces;
using api_be.Extensions;
using api_be.Models.Request;
using api_be.Transforms;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace api_be.Models.ValidatorRequest
{
    public class RefreshTokenValidator : AbstractValidator<BaseTokenRequest>
    {
     

        public RefreshTokenValidator(ISupermarketDbContext context, IConfiguration configuration)
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

            // Validate access token existence and format
            Include(new BaseTokenValidator(context,configuration));
        }

   
}
}
