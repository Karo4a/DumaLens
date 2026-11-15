using Npgsql;

namespace DumaLens.Repositories.Tests.Infrastructure;

/// <summary>
/// Проверяет доступность PostgreSQL для интеграционных тестов (ADR-3):
/// реальное подключение к служебной базе, а не только TCP-порт — иначе неверный пароль
/// роняет тесты вместо явного пропуска
/// </summary>
public static class PostgreSqlAvailability
{
    /// <summary>
    /// Причина пропуска интеграционных тестов; null, если сервер доступен
    /// </summary>
    /// <param name="connectionString">Строка подключения (база подменяется на служебную postgres)</param>
    public static string? GetSkipReason(string connectionString)
    {
        try
        {
            var builder = new NpgsqlConnectionStringBuilder(connectionString)
            {
                Database = "postgres",
                Timeout = 5,
            };
            using var connection = new NpgsqlConnection(builder.ConnectionString);
            connection.Open();
            return null;
        }
        catch (NpgsqlException exception)
        {
            return $"PostgreSQL недоступен: {exception.Message} Интеграционные тесты пропущены (ADR-3): поднимите сервер и повторите прогон.";
        }
        catch (InvalidOperationException exception)
        {
            return $"PostgreSQL недоступен: {exception.Message} Интеграционные тесты пропущены (ADR-3).";
        }
    }
}