using Microsoft.Extensions.Configuration;
using Npgsql;

namespace DumaLens.Api.Tests.Infrastructure;

/// <summary>
/// Проверка доступности локального PostgreSQL (ADR-3)
/// </summary>
internal static class PostgreSqlAvailability
{
    private static readonly Lazy<bool> available = new(CheckConnection);

    /// <summary>
    /// Доступен ли PostgreSQL для интеграционных тестов
    /// </summary>
    internal static bool IsAvailable => available.Value;

    /// <summary>
    /// Строка подключения к служебной базе postgres для проверки доступности
    /// </summary>
    internal static string MaintenanceConnectionString => BuildConnectionString("postgres");

    private static string BuildConnectionString(string database)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.integration.json")
            .AddEnvironmentVariables()
            .Build();
        var template = configuration.GetConnectionString("IntegrationConnection")
            ?? throw new InvalidOperationException("Не настроена строка подключения IntegrationConnection");
        var builder = new NpgsqlConnectionStringBuilder(string.Format(template, Guid.NewGuid().ToString("N")))
        {
            Database = database,
            Timeout = 5
        };
        return builder.ConnectionString;
    }

    private static bool CheckConnection()
    {
        try
        {
            using var connection = new NpgsqlConnection(MaintenanceConnectionString);
            connection.Open();
            return true;
        }
        catch (NpgsqlException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }
}