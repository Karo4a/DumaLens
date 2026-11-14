using DumaLens.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Xunit;

namespace DumaLens.Database.Tests.Infrastructure;

/// <summary>
/// Фикстура одноразовой базы PostgreSQL для тестов структуры и ограничений (ADR-3)
/// </summary>
/// <remarks>
/// База создаётся на прогон коллекции и накатывается миграциями; при недоступном PostgreSQL
/// тесты явно пропускаются (SkipReason), а не падают и не проходят молча.
/// </remarks>
public class DatabaseFixture : IAsyncLifetime
{
    private string? maintenanceConnectionString;
    private string? databaseName;
    private bool databaseCreated;

    /// <summary>
    /// Причина пропуска тестов (PostgreSQL недоступен) или null
    /// </summary>
    public string? SkipReason { get; private set; }

    /// <summary>
    /// Строка подключения к одноразовой базе
    /// </summary>
    public string ConnectionString { get; private set; } = string.Empty;

    /// <summary>
    /// Асинхронная инициализация: создаёт одноразовую базу и накатывает миграции
    /// </summary>
    public async Task InitializeAsync()
    {
        string template;
        try
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.integration.json")
                .AddEnvironmentVariables()
                .Build();
            template = configuration.GetConnectionString("IntegrationConnection")
                ?? throw new InvalidOperationException("Не настроена строка подключения IntegrationConnection");
        }
        catch (Exception ex) when (ex is InvalidOperationException or FileNotFoundException)
        {
            SkipReason = $"PostgreSQL недоступен: {ex.Message}";
            return;
        }

        var builder = new NpgsqlConnectionStringBuilder(string.Format(template, Guid.NewGuid().ToString("N")))
        {
            Timeout = 5,
        };
        ConnectionString = builder.ConnectionString;
        databaseName = builder.Database;
        maintenanceConnectionString = new NpgsqlConnectionStringBuilder(builder.ConnectionString)
        {
            Database = "postgres",
        }.ConnectionString;

        try
        {
            await using var connection = new NpgsqlConnection(maintenanceConnectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = $"CREATE DATABASE \"{databaseName}\"";
            await command.ExecuteNonQueryAsync();
            databaseCreated = true;
        }
        catch (Exception ex) when (ex is NpgsqlException or InvalidOperationException)
        {
            SkipReason = $"PostgreSQL недоступен: {ex.Message}";
            return;
        }

        await using var context = CreateContext();
        await context.Database.MigrateAsync();
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

    /// <summary>
    /// Асинхронное освобождение: удаляет одноразовую базу
    /// </summary>
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
}