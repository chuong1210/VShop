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
    public class BaseTokenValidator : AbstractValidator<BaseTokenRequest>
    {

        public BaseTokenValidator(ISupermarketDbContext context, IConfiguration configuration)
        {
    
            // Validate access token existence and format
            RuleFor(x => x.AccessToken)
                .NotEmpty().WithMessage(IdentityTransform.TokenRequired())
                .Must(token => JwtExtension.BeAValidJwtToken(token, configuration))
                .WithMessage(IdentityTransform.InvalidAccessToken())
                .Must(token => !JwtExtension.IsAccessTokenStillValid(token))
                .WithMessage(IdentityTransform.AccessTokenNotExpired());
        }


    }
}
