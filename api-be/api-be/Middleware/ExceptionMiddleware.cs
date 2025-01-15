﻿using api_be.Constants;
using api_be.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Net;

namespace api_be.Middleware
{
    public class ExceptionMiddleware : IMiddleware
    {
        /// <inheritdoc/>
        public async Task InvokeAsync(HttpContext httpContext, RequestDelegate next)
        {
            try
            {
                do
                {
                    var endpoint = httpContext.GetEndpoint();

                    if (endpoint == null)
                    {
                        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
                        break;
                    }
                    var per = endpoint?.Metadata.GetMetadata<IAuthorizeData>()?.Policy;

                    if (endpoint?.Metadata.GetMetadata<IAllowAnonymous>() != null ||
                        CONSTANT_PERMESSION_DEFAULT.PERMISSIONS_NO_LOGIN.Contains(per))
                    {
                        await next(httpContext);
                        return;
                    }

                    var authorizationHeader = httpContext.Request.Headers["Authorization"].ToString();
                    if ((string.IsNullOrEmpty(authorizationHeader) == false &&
                        authorizationHeader.StartsWith("Bearer ")) == false)
                    {
                        httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        break;
                    }

                    var token = authorizationHeader.Substring("Bearer ".Length).Trim();
                    var tokenHandler = new JwtSecurityTokenHandler();
                    var jwtToken = tokenHandler.ReadToken(token) as JwtSecurityToken;

                    if (jwtToken == null)
                    {
                        break;
                    }

                    // Lấy danh sách quyền của người dùng từ token
                    var permissions = jwtToken.Claims
                                            .Where(c => c.Type == CONSTANT_CLAIM_TYPES.Permission)
                                            .Select(c => c.Value).ToList();

                    var authorizeAttributes = endpoint?.Metadata.GetOrderedMetadata<AuthorizeAttribute>();

                    if ((authorizeAttributes != null && authorizeAttributes.Any()) == false)
                    {
                        break;
                    }

                    var requiredRoles = authorizeAttributes
                                            .SelectMany(attr => (attr.Policy ?? "").Split(','))
                                            .Where(role => !string.IsNullOrEmpty(role))
                                            .Distinct().ToList();

                    if (requiredRoles.Except(permissions).Any())
                    {
                        httpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
                        break;
                    }


                } while (false);

                if (httpContext.Response.StatusCode == StatusCodes.Status401Unauthorized)
                {
                    await httpContext.Response.WriteAsync("Unauthorized!");
                }
                else if (httpContext.Response.StatusCode == StatusCodes.Status403Forbidden)
                {
                    await httpContext.Response.WriteAsync("Forbidden!");
                }
                else if (httpContext.Response.StatusCode == StatusCodes.Status500InternalServerError)
                {
                    await httpContext.Response.WriteAsync("Internal Server Error!");
                }
                else
                {
                    await next(httpContext);
                }
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(httpContext, ex);
                httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await httpContext.Response.WriteAsync("Access Denied: " + ex.Message);
            }
        }

        private Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            HttpStatusCode statusCode = HttpStatusCode.InternalServerError;
            string result = JsonConvert.SerializeObject(new ErrorDeatils
            {
                ErrorMessage = exception.Message,
                ErrorType = "Fail!"
        });

            switch (exception)
            {
                case BadRequestException badRequestException:
                    statusCode = HttpStatusCode.BadRequest;
                    break;
                case ValidationException validationException:
                    statusCode = HttpStatusCode.BadRequest;
                    result = JsonConvert.SerializeObject(validationException.Errors);
                    break;
                case NotFoundException notFoundException:
                    statusCode = HttpStatusCode.NotFound;
                    break;
                default:
                    break;
            }

            context.Response.StatusCode = (int)statusCode;
            return context.Response.WriteAsync(result);
        }
    }

    public class ErrorDeatils
    {
        public string ErrorType { get; set; }
        public string ErrorMessage { get; set; }
    }
}
