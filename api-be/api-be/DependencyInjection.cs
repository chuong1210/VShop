using api_be.Common.Interfaces;
using api_be.Config;
using api_be.DB.Services;
using api_be.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Sieve.Services;
using System.Reflection;

namespace api_be
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddHttpContextAccessor();


            services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

            //services.AddScoped<IPermissionService, PermissionService>();

            services.AddScoped<ICurrentUserService, CurrentUserService>();

            services.AddScoped<ISieveConfiguration, SieveConfiguration>();
            services.AddEndpointsApiExplorer();




            return services;
        }
    }
}
