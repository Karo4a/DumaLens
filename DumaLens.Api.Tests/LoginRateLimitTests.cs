using DumaLens.Api.Tests.Infrastructure;
using DumaLens.Context;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace DumaLens.Api.Tests;

/// <summary>
/// Тест rate limit эндпоинта логина (Э9, 07-security.md §2.2): фиксированное окно на IP,
/// сверх лимита — 429. Отдельный хост с лимитом 3 запроса в окне
/// </summary>
public class LoginRateLimitTests : IAsyncLifetime
{
    private readonly RateLimitFactory factory = new();
    private DumaLensContext? context;
    private bool databaseCreated;

    /// <inheritdoc cref="IAsyncLifetime.InitializeAsync"/>
    public async Task InitializeAsync()
    {
        if (!PostgreSqlAvailability.IsAvailable)
        {
            return;
        }
        var scope = factory.Services.GetRequiredService<IServiceScopeFactory>().CreateScope();
        context = scope.ServiceProvider.GetRequiredService<DumaLensContext>();
        await context.Database.MigrateAsync();
        databaseCreated = true;
    }

    /// <inheritdoc cref="IAsyncLifetime.DisposeAsync"/>
    public async Task DisposeAsync()
    {
        if (databaseCreated && context != null)
        {
            var database = context.Database.GetDbConnection().Database;
            await using (var connection = new NpgsqlConnection(PostgreSqlAvailability.MaintenanceConnectionString))
            {
                await connection.OpenAsync();
                await using var command = connection.CreateCommand();
                command.CommandText = "DROP DATABASE IF EXISTS \"" + database + "\" WITH (FORCE)";
                await command.ExecuteNonQueryAsync();
            }
            await context.Database.CloseConnectionAsync();
            await context.DisposeAsync();
        }
        await factory.DisposeAsync();
    }

    /// <summary>
    /// Четвёртый запрос логина в окне отклоняется с 429
    /// </summary>
    [SkippableFact]
    public async Task FourthLoginInWindowShouldReturn429()
    {
        Skip.IfNot(PostgreSqlAvailability.IsAvailable, "PostgreSQL недоступен — интеграционный тест пропущен (ADR-3)");
        var client = factory.CreateClient();
        for (var i = 0; i < 3; i++)
        {
            var response = await client.PostAsJsonAsync("/api/auth/login", new { login = "nobody", password = "WrongPassword1" });
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized, "запрос {0} в пределах лимита", i + 1);
        }
        var rejected = await client.PostAsJsonAsync("/api/auth/login", new { login = "nobody", password = "WrongPassword1" });
        rejected.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    /// <summary>
    /// Смена пароля защищена собственным rate limit: четвёртый запрос в окне — 429
    /// (брутфорс текущего пароля украденным access-токеном ограничен)
    /// </summary>
    [SkippableFact]
    public async Task FourthChangePasswordInWindowShouldReturn429()
    {
        Skip.IfNot(PostgreSqlAvailability.IsAvailable, "PostgreSQL недоступен — интеграционный тест пропущен (ADR-3)");
        var client = factory.CreateClient();
        var login = $"rl_change_{Guid.NewGuid():N}";
        await AuthApiHelpers.SeedUser(context!, login);
        var tokens = await client.LoginAsync(login);
        var accessToken = tokens.GetProperty("accessToken").GetString()!;

        for (var i = 0; i < 3; i++)
        {
            var response = await client.PostWithBearer("/api/auth/change-password",
                new { currentPassword = "WrongPassword1", newPassword = "NewPassword456" }, accessToken);
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized, "запрос {0} в пределах лимита", i + 1);
        }
        var rejected = await client.PostWithBearer("/api/auth/change-password",
            new { currentPassword = "WrongPassword1", newPassword = "NewPassword456" }, accessToken);
        rejected.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    private sealed class RateLimitFactory : TestWebApplicationFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Auth:LoginRateLimitPermitLimit"] = "3",
                ["Auth:LoginRateLimitWindowSeconds"] = "60",
            }));
        }
    }
}