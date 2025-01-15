using api_be.DB.Interceptors;
using api_be.DB.Services;
using api_be.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Persistence.BusinessData;

namespace api_be.DB
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
