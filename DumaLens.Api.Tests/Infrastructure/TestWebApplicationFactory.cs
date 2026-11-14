using DumaLens.Context;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DumaLens.Api.Tests.Infrastructure;

/// <summary>
/// Фабрика для интеграционных тестов (PostgreSQL, ADR-3)
/// </summary>
public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    /// <summary>
    /// Имя одноразовой базы данных теста
    /// </summary>
    internal string DatabaseName { get; } = $"dumalens_{Guid.NewGuid():N}";

    /// <inheritdoc cref="WebApplicationFactory{TEntryPoint}.ConfigureWebHost"/>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestAppConfiguration();
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<DumaLensContext>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }
            services.AddSingleton(provider =>
            {
                var configuration = provider.GetRequiredService<IConfiguration>();
                var template = configuration.GetConnectionString("IntegrationConnection");
                var optionsBuilder = new DbContextOptionsBuilder<DumaLensContext>()
                    .UseApplicationServiceProvider(provider)
                    .UseNpgsql(string.Format(template!, DatabaseName));
                return optionsBuilder.Options;
            });
        });
    }
}