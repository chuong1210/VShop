using api_be.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Sieve.Services;
using System.Reflection;
using api_be.Application.Services.Imps;
using api_be.Application.Services;
using AutoMapper;
using api_be.Application.Mapper;
using api_be.Middleware;
using Microsoft.AspNetCore.Authorization;
using api_be.Core.Domain.Interfaces;
using api_be.Core.Entities.Auth;
using api_be.Infrastructure.DB;
using CloudinaryDotNet;
using Elastic.Clients.Elasticsearch;
using api_be.Application.Services.KafkaService;
using api_be.Infrastructure.DB.Interceptors;
using api_be.Core.Entities;
using api_be.Domain.Config;

namespace api_be
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
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


            //--------------------------Kafka---------------------------
            //services.AddSingleton<KafkaProducer>();
            //services.AddHostedService<KafkaProducerService>();
            //var kafkaService = services.BuildServiceProvider().GetService<KafkaProducer>();
            //kafkaService.CreateTopic(configuration["Kafka:ProductTopic"]);
            //services.AddHostedService<KafkaConsumerService>();
            //---------------------------------------------
            services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<ICategoryService, CategoryService>();
            var elasticClient = new ElasticsearchClient(new ElasticsearchClientSettings(new Uri(configuration["Elasticsearch:Url"])));

          
            services.AddSingleton<ElasticsearchClient>(sp =>
            {
                var settings = new ElasticsearchClientSettings(new Uri(configuration["Elasticsearch:Url"]))
                    .DefaultIndex(configuration["Elasticsearch:DefaultIndex"]);
                return new ElasticsearchClient(settings);
            });

            services.AddSingleton(typeof(KafkaProducer<,>));

            services.AddSingleton<ProductKafkaConsumer>();

          
            services.AddScoped<IEmailService, EmailService>();
            services.AddMemoryCache();
            services.AddHttpClient(); // Thêm dòng này để đăng ký IHttpClientFactory


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


            //services.AddHostedService<KafkaStartupService>();

            services.AddHostedService<ProductEventConsumerService>();

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
