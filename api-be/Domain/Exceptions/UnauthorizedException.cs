using Microsoft.AspNetCore.Http;
using api_be.Domain.Transforms;
namespace api_be.Domain.Exceptions
{
    public class UnauthorizedException : ApplicationException
    {
        public int ErrorCode { get; } = StatusCodes.Status401Unauthorized;

        public UnauthorizedException(int errorCode) : base(IdentityTransform.ForbiddenException())
        {
            ErrorCode = errorCode == StatusCodes.Status403Forbidden ? StatusCodes.Status403Forbidden : ErrorCode;
        }
    }

}
