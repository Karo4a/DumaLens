using System.Net.Sockets;

namespace DumaLens.Importer.Tests.Infrastructure;

/// <summary>
/// Проверяет доступность PostgreSQL для интеграционных тестов (ADR-3)
/// </summary>
public static class PostgreSqlAvailability
{
    /// <summary>
    /// Причина пропуска интеграционных тестов; null, если сервер доступен
    /// </summary>
    public static string? GetSkipReason(string connectionString)
    {
        var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var host = "localhost";
        var port = 5432;
        foreach (var part in parts)
        {
            var separator = part.IndexOf('=');
            if (separator <= 0)
            {
                continue;
            }

            var key = part[..separator].Trim();
            var value = part[(separator + 1)..].Trim();
            if (key.Equals("Host", StringComparison.OrdinalIgnoreCase))
            {
                host = value;
            }
            else if (key.Equals("Port", StringComparison.OrdinalIgnoreCase) && int.TryParse(value, out var parsedPort))
            {
                port = parsedPort;
            }
        }

        try
        {
            using var client = new TcpClient();
            var connect = client.BeginConnect(host, port, null, null);
            if (!connect.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(2)) || !client.Connected)
            {
                return $"PostgreSQL недоступен ({host}:{port}). Интеграционные тесты пропущены (ADR-3): поднимите сервер и повторите прогон.";
            }

            client.EndConnect(connect);
            return null;
        }
        catch (Exception exception)
        {
            return $"PostgreSQL недоступен ({host}:{port}): {exception.Message}. Интеграционные тесты пропущены (ADR-3).";
        }
    }
}