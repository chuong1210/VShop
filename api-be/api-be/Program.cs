using api_be;
using api_be.DB;
using api_be.Middleware;
using Microsoft.AspNetCore.Authorization;
using System.Reflection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using api_be.Domain.Interfaces;
using System.IO;
using api_be.Services.Imps;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using System.Security.Claims;
using System.Text.Json.Serialization;
using CloudinaryDotNet;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
builder.Configuration.AddJsonFile("client_secrets.json", optional: false, reloadOnChange: true);

// Lấy các thông tin cấu hình từ `client_secrets.json`
var googleConfig = builder.Configuration.GetSection("Authentication:Google");
var githubConfig = builder.Configuration.GetSection("Authentication:GitHub");
var cloudinaryConfig = builder.Configuration.GetSection("Authentication:Cloudinary");
var facebookConfig = builder.Configuration.GetSection("Authentication:Facebook");

// Add services to the container.
var JWTSetting = builder.Configuration.GetSection("JWTSetting");


builder.Services.AddRouting(options => options.LowercaseUrls = true);

builder.Services.AddSignalR();


builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = builder.Configuration.GetValue<string>("JwtSettings:Issuer"),
                        ValidAudience = builder.Configuration.GetValue<string>("JwtSettings:Audience"),
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:Key"]))
                    };


                    options.SaveToken = true;

                    options.Events = new JwtBearerEvents
                    {
                        OnAuthenticationFailed = context =>
                        {
                            Console.WriteLine($"Authentication failed: {context.Exception.Message}");
                            // Important:  Log the specific error for debugging
                            return Task.CompletedTask;
                        },
                        OnTokenValidated = context =>
                        {
                            Console.WriteLine("Token is valid.");
                            return Task.CompletedTask;
                        },
                        OnMessageReceived = context =>
                        {
                            // Nhận token từ query string cho SignalR
                            var accessToken = context.Request.Query["access_token"];
                            if (!string.IsNullOrEmpty(accessToken))
                            {
                                var path = context.HttpContext.Request.Path;

                                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/chatHub"))
                                {
                                    context.Token = accessToken;
                                }
                            }
                            // het signal r
                            // Important: Handle potential message errors
                            // Check if the message contains a JWT
                            if (context.HttpContext.Request.Headers.TryGetValue("Authorization", out var authHeader))
                            {
                                // Validate if the token is correctly formatted
                                string authHeaderValue = authHeader.ToString();
                                if (authHeaderValue.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                                {
                                    var token = authHeaderValue.Substring("Bearer ".Length);
                                    // Try to parse the token, catch potential exceptions
                                    try
                                    {
                                        context.Token = token;
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"Error parsing token: {ex.Message}");

                                    }
                                }
                            }
                            return Task.CompletedTask;
                        }

                    };
                    options.RequireHttpsMetadata = false;

                })
                .AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
                {
                    options.ClientId = builder.Configuration["Authentication:Google:ClientId"];
                    options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
                })
                .AddOAuth("GitHub", options =>
                {
                    options.ClientId = builder.Configuration["Authentication:GitHub:ClientId"];
                    options.ClientSecret = builder.Configuration["Authentication:GitHub:ClientSecret"];
                    options.CallbackPath = new PathString("/signin-github");

                    options.AuthorizationEndpoint = "https://github.com/login/oauth/authorize";
                    options.TokenEndpoint = "https://github.com/login/oauth/access_token";
                    options.UserInformationEndpoint = "https://api.github.com/user";
                    options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "id");
                    options.ClaimActions.MapJsonKey(ClaimTypes.Name, "login");

                    options.ClaimActions.MapJsonKey("urn:github:login", "login");
                    options.ClaimActions.MapJsonKey("urn:github:url", "html_url");
                    options.ClaimActions.MapJsonKey("urn:github:avatar", "avatar_url");
                    options.ClaimActions.MapJsonKey("urn:github:email", "email");


                    options.Events = new OAuthEvents
                    {
                        OnCreatingTicket = async context =>
                        {
                            var request = new HttpRequestMessage(System.Net.Http.HttpMethod.Get, context.Options.UserInformationEndpoint);
                            request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", context.AccessToken);

                            var response = await context.Backchannel.SendAsync(request);
                            response.EnsureSuccessStatusCode();

                            var user = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                            context.RunClaimActions(user.RootElement);
                        }
                    };
                });
                    
                


builder.Services.AddSingleton<IAuthorizationHandler, PermissionRequirementHandler>();
builder.Services.AddSingleton(sp =>
{
    var config = builder.Configuration.GetSection("Cloudinary");
    return new Cloudinary(new Account(
        config["CloudName"],
        config["ApiKey"],
        config["ApiSecret"]
    ));
});

builder.Services.AddAuthorization(options =>
{
    options.AddPermissionPoliciesFromAttributes(Assembly.GetExecutingAssembly());

    //options.AddPolicy("AdminOnly", policy => policy.RequireClaim("type", "Admin"));
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));



});



builder.Services.AddTransient<ExceptionMiddleware>();

//builder.Services.AddControllers();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        //options.JsonSerializerOptions.MaxDepth = 3; // Set the desired maximum depth
        //options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.Preserve;

    });

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddPersistenceBusinessDataServices(builder.Configuration);

builder.Services.AddCors(p => p.AddPolicy("MyCors", build =>
{
    build.WithOrigins("*")
         .AllowAnyMethod()
         .AllowAnyHeader();
}));
//builder.Services.AddControllers()
//    .AddFluentValidation(fv => fv.RegisterValidatorsFromAssemblyContaining<Program>());

var app = builder.Build();

// Configure the HTTP request pipeline.

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();

    // Initialise and seed database
    using var scope = app.Services.CreateScope();
    var initializer = scope.ServiceProvider.GetRequiredService<SupermarketDbContextInitialiser>();
    await initializer.InitializeAsync();
    InitializePermissions(builder.Services.BuildServiceProvider()).GetAwaiter().GetResult();
    await initializer.SeedAsync();

    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Supermarket.Api v1"));
}
else
{
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseCors("MyCors");

app.UseRouting();

app.UseMiddleware<ExceptionMiddleware>();
app.UseAuthentication();


app.UseAuthorization();


app.UseEndpoints(endpoints =>
{
    endpoints.MapHub<ChatHubService>("/chatHub"); // Đăng ký Hub tại endpoint "/chatHub"
});

app.MapControllers();

app.Run();


async Task InitializePermissions(IServiceProvider serviceProvider)
{
    var permissionService = serviceProvider.GetRequiredService<IPermissionService>();

    List<string> permissions = AuthorizationExtensions
            .GetPermissionPoliciesFromAttributes(Assembly.GetExecutingAssembly());
    await permissionService.Create(permissions);
}

//dotnet ef migrations add InitialTable --context SupermarketDbContext --output-dir DB/Migrations
//dotnet ef database update --context SupermarketDbContext


