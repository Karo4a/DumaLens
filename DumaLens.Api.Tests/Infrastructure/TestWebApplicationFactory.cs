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
    /// Идентификатор одноразовой базы данных теста (подставляется в шаблон строки подключения один раз)
    /// </summary>
    internal string DatabaseId { get; } = Guid.NewGuid().ToString("N");

    /// <inheritdoc cref="WebApplicationFactory{TEntryPoint}.ConfigureWebHost"/>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestAppConfiguration();
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            // HTTP-тесты отзыва при повторном использовании детерминированы: grace-окно отключено
            // (поведение окна проверяется в Services.Tests с управляемыми часами)
            ["Auth:RefreshReuseGraceSeconds"] = "0",
            // Функциональные тесты не должны упираться в rate limit общего хоста (все запросы с одного IP);
            // поведение лимита проверяется в LoginRateLimitTests на отдельном хосте с лимитом 3
            ["Auth:LoginRateLimitPermitLimit"] = "10000",
        }));
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
                    .UseNpgsql(string.Format(template!, DatabaseId));
                return optionsBuilder.Options;
            });
        });
    }
}