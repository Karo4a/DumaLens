using DumaLens.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace DumaLens.Importer.Tests.Infrastructure;

/// <summary>
/// Одноразовая PostgreSQL-база для интеграционных тестов: создаётся с миграциями перед прогоном, удаляется после
/// </summary>
public sealed class DatabaseFixture : IAsyncLifetime
{
    /// <summary>
    /// Строка подключения к одноразовой базе
    /// </summary>
    public string ConnectionString { get; private set; } = string.Empty;

    /// <summary>
    /// Причина пропуска интеграционных тестов; null, если сервер доступен
    /// </summary>
    public string? SkipReason { get; private set; }

    /// <inheritdoc cref="IAsyncLifetime.InitializeAsync"/>
    public async Task InitializeAsync()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.integration.json", optional: false)
            .AddEnvironmentVariables()
            .Build();

        var template = configuration.GetConnectionString("IntegrationConnection")
            ?? throw new InvalidOperationException("Не задана строка подключения IntegrationConnection (appsettings.integration.json).");

        ConnectionString = string.Format(template, $"integration_{Guid.NewGuid():N}");
        SkipReason = PostgreSqlAvailability.GetSkipReason(ConnectionString);
        if (SkipReason != null)
        {
            return;
        }

        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    /// <inheritdoc cref="IAsyncLifetime.DisposeAsync"/>
    public async Task DisposeAsync()
    {
        if (SkipReason != null)
        {
            return;
        }

        await using var context = CreateContext();
        await context.Database.EnsureDeletedAsync();
    }

    /// <summary>
    /// Создаёт контекст одноразовой базы
    /// </summary>
    public DumaLensContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<DumaLensContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        return new DumaLensContext(options);
    }
}