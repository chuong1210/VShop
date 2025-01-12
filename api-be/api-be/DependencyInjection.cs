using api_be.Common.Interfaces;
using api_be.Config;
using api_be.DB.Services;
using api_be.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Sieve.Services;
using System.Reflection;
using api_be.Services.Imps;
using api_be.Services;
using AutoMapper;
using api_be.Mapping;

namespace api_be
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddHttpContextAccessor();

            //services.AddSingleton(provider => new MapperConfiguration(cfg =>
            //{
            //    cfg.AddProfile(new MappingProfile());
            //}).CreateMapper());

            services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());



            services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
            services.AddScoped<IAuthService, AuthService>();


            services.AddScoped<IPermissionService, PermissionService>();

            services.AddScoped<ICurrentUserService, CurrentUserService>();

            services.AddScoped<ISieveConfiguration, SieveConfiguration>();
            services.AddEndpointsApiExplorer();




            return services;
        }
    }
}
