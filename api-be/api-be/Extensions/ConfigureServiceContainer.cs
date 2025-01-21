

using api_be.DB;
using api_be.Domain.Interfaces;
using api_be.Mapper;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace api_be.Extensions
{
    public static class ConfigureServiceContainer
    {
        public static void AddDbContext(this IServiceCollection serviceCollection,
             IConfiguration configuration, IConfigurationRoot configRoot)
        {
            serviceCollection.AddDbContext<SupermarketDbContext>(options =>
                   options.UseSqlServer(configuration.GetConnectionString("OAuth2ConnectString") ?? configRoot["ConnectionStrings:OAuth2ConnectString"]
                , b => b.MigrationsAssembly(typeof(SupermarketDbContext).Assembly.FullName)));


        }

        public static void AddAutoMapper(this IServiceCollection serviceCollection)
        {
            var mappingConfig = new MapperConfiguration(mc =>
            {
                mc.AddProfile(new MappingProfile());
            });
            IMapper mapper = mappingConfig.CreateMapper();
            serviceCollection.AddSingleton(mapper);
        }

        public static void AddScopedServices(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<ISupermarketDbContext>(provider => provider.GetService<SupermarketDbContext>());
        }

        public static void AddAuthentications(this IServiceCollection serviceCollection,
             IConfiguration configuration, IConfigurationRoot configRoot)
        {
            serviceCollection.AddAuthentication()
                             .AddGoogle(options =>
                             {
                                 options.ClientId = configRoot["web:client_id"];
                                 options.ClientSecret = configRoot["web:client_secret"];
                             })
                             .AddFacebook(options =>
                             {
                                 options.AppId = configRoot["web:client_id"];
                                 options.ClientSecret = configRoot["web:client_secret"];
                             });
        }

        //public static void AddTransientServices(this IServiceCollection serviceCollection)
        //{
        //    serviceCollection.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

        //    serviceCollection.AddTransient<IDateTimeService, DateTimeService>();
        //    serviceCollection.AddTransient<IAccountService, AccountService>();
        //    serviceCollection.AddTransient<IRoleService, RoleService>();
        //    serviceCollection.AddTransient<IPermissionService, PermissionService>();
        //    serviceCollection.AddTransient<IUserService, UserService>();
        //}

     
        //public static void AddController(this IServiceCollection serviceCollection)
        //{
        //    serviceCollection.AddControllers().AddNewtonsoftJson();
        //}

        //public static void AddVersion(this IServiceCollection serviceCollection)
        //{
        //    serviceCollection.AddApiVersioning(config =>
        //    {
        //        config.DefaultApiVersion = new ApiVersion(1, 0);
        //        config.AssumeDefaultVersionWhenUnspecified = true;
        //        config.ReportApiVersions = true;
        //    });
        //}

        //public static void AddHealthCheck(this IServiceCollection serviceCollection, AppSettings appSettings, IConfiguration configuration)
        //{
        //    serviceCollection.AddHealthChecks()
        //        .AddDbContextCheck<OAuth2DbContext>(name: "Application DB Context", failureStatus: HealthStatus.Degraded)
        //        .AddUrlGroup(new Uri(appSettings.ApplicationDetail.ContactWebsite), name: "My personal website", failureStatus: HealthStatus.Degraded)
        //        .AddSqlServer(configuration.GetConnectionString("OAuth2ConnectString"));

        //    serviceCollection.AddHealthChecksUI(setupSettings: setup =>
        //    {
        //        setup.AddHealthCheckEndpoint("Basic Health Check", $"/healthz");
        //    }).AddInMemoryStorage();
        //}

    }
}
