using DumaLens.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace DumaLens.Importer.Tests.Infrastructure;

/// <summary>
/// Одноразовая PostgreSQL-база для интеграционных тестов: создаётся с миграциями перед прогоном, удаляется после
/// </summary>
/// <remarks>
/// База создаётся явным CREATE DATABASE и удаляется DROP DATABASE ... WITH (FORCE) через служебное
/// подключение к postgres. FORCE обязателен: после COPY-импорта и EF-контекстов в пуле Npgsql остаются
/// открытые соединения, и обычный DROP (как EnsureDeletedAsync) падает «база занята другими пользователями»,
/// оставляя базу-сироту. При недоступном PostgreSQL тесты явно пропускаются (SkipReason), а не падают молча.
/// </remarks>
public sealed class DatabaseFixture : IAsyncLifetime
{
    private string? maintenanceConnectionString;
    private string? databaseName;
    private bool databaseCreated;

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

        var builder = new NpgsqlConnectionStringBuilder(string.Format(template, $"integration_{Guid.NewGuid():N}"))
        {
            Timeout = 5,
        };
        ConnectionString = builder.ConnectionString;
        databaseName = builder.Database;
        maintenanceConnectionString = new NpgsqlConnectionStringBuilder(builder.ConnectionString)
        {
            Database = "postgres",
        }.ConnectionString;

        SkipReason = PostgreSqlAvailability.GetSkipReason(ConnectionString);
        if (SkipReason != null)
        {
            return;
        }

        await using var connection = new NpgsqlConnection(maintenanceConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"CREATE DATABASE \"{databaseName}\"";
        await command.ExecuteNonQueryAsync();
        databaseCreated = true;

        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    /// <inheritdoc cref="IAsyncLifetime.DisposeAsync"/>
    public async Task DisposeAsync()
    {
        if (!databaseCreated)
        {
            return;
        }

        await using var connection = new NpgsqlConnection(maintenanceConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"DROP DATABASE IF EXISTS \"{databaseName}\" WITH (FORCE)";
        await command.ExecuteNonQueryAsync();
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