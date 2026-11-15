using AutoMapper;
using DumaLens.Api.Infrastructure;
using DumaLens.Common;
using DumaLens.Common.Contracts;
using DumaLens.Common.Extensions;
using DumaLens.Context;
using DumaLens.Context.Contracts;
using DumaLens.Repositories;
using DumaLens.Services;
using DumaLens.Services.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Text.Json.Serialization;

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
        var app = builder.Build();
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }
        app.UseHttpsRedirection();
        app.UseRouting();
        app.UseCors("AllowBlazorClient");
        app.MapControllers();
        app.MapHealthChecks("/health");
        app.Run();
    }
}