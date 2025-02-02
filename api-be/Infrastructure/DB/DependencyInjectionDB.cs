using api_be.Infrastructure.DB.Interceptors;
using api_be.Core.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using api_be.Infrastructure.Services;

namespace api_be.Infrastructure.DB
{
    public static class DependencyInjectionDB
    {
        public static IServiceCollection AddPersistenceBusinessDataServices(this IServiceCollection services, IConfiguration config)
        {
            services.AddScoped<EntitySaveChangesInterceptor>();

            services.AddDbContext<ISupermarketDbContext, SupermarketDbContext>(options =>
                options.UseSqlServer(config.GetConnectionString("VSHOPConnect"), builder =>
                {
                    //builder.MigrationsAssembly("DB");  // Chỉ định rõ assembly nơi chứa các migration

                    builder.MigrationsAssembly(typeof(DependencyInjectionDB).Assembly.FullName);
                    builder.EnableRetryOnFailure();
                }));


            services.AddScoped<SupermarketDbContextInitialiser>();

            services.AddSingleton<IDateTimeService, DateTimeService>();

            return services;
        }
    }

}
