using api_be.Config;
using api_be.DB.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Sieve.Services;
using System.Reflection;
using api_be.Services.Imps;
using api_be.Services;
using AutoMapper;
using api_be.Mapping;
using api_be.Middleware;
using Microsoft.AspNetCore.Authorization;
using api_be.Domain.Interfaces;
using api_be.Entities.Auth;
using api_be.DB;
using CloudinaryDotNet;

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
            //    cfg.AddProfile(new CommonMappingProfile());

            //}).CreateMapper());

            services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
            services.AddScoped<ISieveProcessor, SieveProcessor>();

            services.AddScoped<ISieveConfiguration, SieveConfiguration>();


            services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<ICategoryService, CategoryService>();


            services.AddScoped<IEmailService, EmailService>();

            RegisterAllServices(services);

            //services.AddScoped<ISupermarketDbContext>(provider => provider.GetService<SupermarketDbContext>());


            //services.AddAuthorization(options =>
            //{
            //    options.AddPolicy("RolePolicy", policy =>
            //    {
            //        policy.Requirements.Add(new RoleRequirement("Admin"));
            //    });
            //});
            //services.AddSingleton<IAuthorizationHandler, RoleRequirementHandler>();


            services.AddScoped<IPermissionService, PermissionService>();

            services.AddScoped<ICurrentUserService, CurrentUserService>();


            services.AddEndpointsApiExplorer();




            return services;
        }




        private static void RegisterAllServices(IServiceCollection services)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var assembly in assemblies)
            {
                var typesWithAttribute = assembly.GetTypes()
                    .Where(type => type.IsClass && !type.IsAbstract &&
                                   type.GetCustomAttributes(typeof(RegisterServiceAttribute), true).Any());

                foreach (var type in typesWithAttribute)
                {
                    var attribute = type.GetCustomAttribute<RegisterServiceAttribute>();
                    var serviceInterfaces = type.GetInterfaces();

                    foreach (var serviceInterface in serviceInterfaces)
                    {
                        switch (attribute.Lifetime)
                        {
                            case ServiceLifetime.Singleton:
                                services.AddSingleton(serviceInterface, type);
                                break;
                            case ServiceLifetime.Scoped:
                                services.AddScoped(serviceInterface, type);
                                break;
                            case ServiceLifetime.Transient:
                                services.AddTransient(serviceInterface, type);
                                break;
                        }
                    }
                }
            }
        }
        }
}
