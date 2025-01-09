using api_be.Common.Interfaces;
using api_be.Config;
using Core.Domain.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Sieve.Services;
using System.Reflection;

namespace Core.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
        

            services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

            //services.AddScoped<IPermissionService, IPermissionService>();

            services.AddScoped<ISieveConfiguration, SieveConfiguration>();




            return services;
        }
    }
}
