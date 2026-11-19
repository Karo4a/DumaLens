using AutoMapper;
using DumaLens.Api.Controllers;
using DumaLens.Api.Infrastructure;
using DumaLens.Api.Infrastructure.Authorization;
using DumaLens.Common;
using DumaLens.Common.Contracts;
using DumaLens.Common.Extensions;
using DumaLens.Context;
using DumaLens.Context.Contracts;
using DumaLens.Entities.Enums;
using DumaLens.Repositories;
using DumaLens.Services;
using DumaLens.Services.Contracts;
using DumaLens.Services.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

namespace DumaLens.Api;

/// <summary>
/// Класс программы
/// </summary>
public class Program
{
    /// <summary>
    /// Точка входа программы
    /// </summary>
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var controllers = builder.Services.AddControllers(opt =>
        {
            opt.Filters.Add<DumaLensExceptionFilter>();
        })
        .AddJsonOptions(opt =>
        {
            // Enum контракта §04.5 — строковые имена (ThirdReading, Accepted, ...)
            opt.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });
        if (builder.Environment.EnvironmentName == EnvironmentProvider.IntegrationEnvironment)
        {
            controllers.AddControllersAsServices();
        }
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Введите ваш JWT токен"
            });
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
            var baseDirectory = AppContext.BaseDirectory;
            c.IncludeXmlComments(Path.Combine(baseDirectory, "DumaLens.Api.xml"));
            c.IncludeXmlComments(Path.Combine(baseDirectory, "DumaLens.Api.Models.xml"));
            c.SchemaFilter<EnumNameSchemaFilter>();
            c.EnableAnnotations();
            // allOf-обёртка для $ref-свойств: OpenAPI 3.0 игнорирует description рядом с голым $ref,
            // из-за чего XML-описания enum-свойств (stage/outcome/source/result) терялись в спеке
            c.UseAllOfToExtendReferenceSchemas();
            c.CustomOperationIds(apiDescription =>
                $"{apiDescription.ActionDescriptor.RouteValues["controller"]}_{apiDescription.ActionDescriptor.RouteValues["action"]}");
        });
        builder.Services.AddHealthChecks();
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
        builder.Services.AddDbContext<DumaLensContext>(options =>
            options.UseNpgsql(connectionString));
        builder.Services.AddScoped<IReader>(x => x.GetRequiredService<DumaLensContext>());
        builder.Services.AddScoped<IWriter>(x => x.GetRequiredService<DumaLensContext>());
        builder.Services.AddScoped<IUnitOfWork>(x => x.GetRequiredService<DumaLensContext>());
        builder.Services.RegisterAssemblyInterfacesAssignableTo<IServiceAnchor>(ServiceLifetime.Scoped);
        builder.Services.RegisterAssemblyInterfacesAssignableTo<IRepositoryAnchor>(ServiceLifetime.Scoped);
        builder.Services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        builder.Services.AddSingleton(_ =>
        {
            var mapConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<ServiceProfile>();
                cfg.AddProfile<ApiMapper>();
            });
            var mapper = mapConfig.CreateMapper();
            return mapper;
        });
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? ["http://localhost:5100", "https://localhost:5101"];
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowBlazorClient", policy =>
            {
                policy.WithOrigins(allowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });
        // Аутентификация и роли (Э9, 07-security.md): JWT JWS HMAC-SHA256, иерархия ролей, rate limit логина.
        // AuthOptions связывается лениво из DI: конфигурация тестового хоста применяется после регистрации сервисов в Main
        builder.Services.AddSingleton(provider =>
        {
            var configuration = provider.GetRequiredService<IConfiguration>();
            var authOptions = configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>() ?? new AuthOptions();
            if (string.IsNullOrWhiteSpace(authOptions.SigningKey))
            {
                if (provider.GetRequiredService<IWebHostEnvironment>().IsProduction())
                {
                    throw new InvalidOperationException("Auth:SigningKey не настроен: задайте ключ подписи JWT (base64, не менее 32 байт) через переменную окружения Auth__SigningKey или user-secrets (О-7)");
                }
                // Временный ключ на время жизни процесса: сессии инвалидируются при перезапуске.
                // Для развёртывания ключ генерируется один раз и хранится в конфигурации (Docs/08-deployment.md)
                authOptions.SigningKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
                provider.GetRequiredService<ILogger<Program>>().LogWarning("Auth:SigningKey не настроен — используется временный ключ на время жизни процесса; сессии не переживут перезапуск. Для production задайте Auth__SigningKey");
            }
            return authOptions;
        });
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();
        builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<AuthOptions>((options, authOptions) =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = authOptions.Issuer,
                    ValidateAudience = true,
                    ValidAudience = authOptions.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Convert.FromBase64String(authOptions.SigningKey)),
                    RoleClaimType = ClaimTypes.Role,
                    NameClaimType = ClaimTypes.Name,
                };
            });
        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy(AuthorizationPolicies.Moderator, policy =>
                policy.Requirements.Add(new MinimumRoleRequirement(Role.Moderator)));
            options.AddPolicy(AuthorizationPolicies.Administrator, policy =>
                policy.Requirements.Add(new MinimumRoleRequirement(Role.Administrator)));
        });
        builder.Services.AddSingleton<IAuthorizationHandler, MinimumRoleHandler>();
        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(AuthController.LoginRateLimitPolicy, httpContext =>
            {
                var authOptions = httpContext.RequestServices.GetRequiredService<AuthOptions>();
                return RateLimitPartition.GetFixedWindowLimiter(
                    httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = authOptions.LoginRateLimitPermitLimit,
                        Window = TimeSpan.FromSeconds(authOptions.LoginRateLimitWindowSeconds),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0,
                    });
            });
        });
        var app = builder.Build();
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }
        // Редирект на HTTPS включается только при настроенном https-эндпоинте:
        // демо-комплект и http-профиль работают по HTTP, иначе middleware пишет
        // предупреждение "Failed to determine the https port for redirect"
        var urls = app.Configuration["ASPNETCORE_URLS"] ?? app.Configuration["urls"];
        if (urls is not null && urls.Contains("https://", StringComparison.OrdinalIgnoreCase))
        {
            app.UseHttpsRedirection();
        }
        app.UseRouting();
        app.UseCors("AllowBlazorClient");
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        app.MapHealthChecks("/health");
        app.Run();
    }
}