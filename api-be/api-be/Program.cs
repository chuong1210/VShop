


using api_be;
using api_be.DB;
using api_be.Middleware;
using Microsoft.AspNetCore.Authorization;
using System.Reflection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using api_be.Domain.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.


builder.Services.AddRouting(options => options.LowercaseUrls = true);

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

                });


builder.Services.AddSingleton<IAuthorizationHandler, PermissionRequirementHandler>();

builder.Services.AddAuthorization(options =>
{
    options.AddPermissionPoliciesFromAttributes(Assembly.GetExecutingAssembly());

    //options.AddPolicy("AdminOnly", policy => policy.RequireClaim("type", "Admin"));

});



builder.Services.AddTransient<ExceptionMiddleware>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddApplicationServices();
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
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseAuthentication();

app.UseHttpsRedirection();
app.UseCors("MyCors");

app.UseRouting();

app.UseMiddleware<ExceptionMiddleware>();


app.UseAuthorization();

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


