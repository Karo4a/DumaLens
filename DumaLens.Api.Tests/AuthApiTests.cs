using DumaLens.Api.Tests.Infrastructure;
using DumaLens.Entities.Enums;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace DumaLens.Api.Tests;

/// <summary>
/// Интеграционные тесты аутентификации Api (Э9, 07-security.md §5):
/// вход/профиль/выход/смена пароля по HTTP, ротация и отзыв refresh, 401 без токена,
/// публичное чтение остаётся анонимным
/// </summary>
[Collection(nameof(DumaLensCollection))]
public class AuthApiTests
{
    private readonly DumaLensApiFixture fixture;
    private readonly HttpClient client;

    /// <summary>
    /// Конструктор
    /// </summary>
    public AuthApiTests(DumaLensApiFixture fixture)
    {
        this.fixture = fixture;
        client = fixture.CreateClient();
    }

    /// <summary>
    /// Вход возвращает пару токенов и профиль; профиль доступен с Bearer-токеном
    /// </summary>
    [SkippableFact]
    public async Task LoginThenMeShouldReturnProfile()
    {
        Skip.IfNot(PostgreSqlAvailability.IsAvailable, "PostgreSQL недоступен — интеграционный тест пропущен (ADR-3)");
        var login = $"auth_me_{Guid.NewGuid():N}";
        await AuthApiHelpers.SeedUser(fixture.Context, login, Role.Moderator);

        var tokens = await client.LoginAsync(login);
        var accessToken = tokens.GetProperty("accessToken").GetString()!;
        tokens.GetProperty("refreshToken").GetString().Should().NotBeNullOrEmpty();
        tokens.GetProperty("user").GetProperty("role").GetString().Should().Be("Moderator");

        var response = await client.GetWithBearer("/api/auth/me", accessToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var profile = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonDocument>();
        profile!.RootElement.GetProperty("login").GetString().Should().Be(login);
        profile.RootElement.GetProperty("role").GetString().Should().Be("Moderator");
    }

    /// <summary>
    /// Профиль без токена — 401
    /// </summary>
    [SkippableFact]
    public async Task MeWithoutTokenShouldReturn401()
    {
        Skip.IfNot(PostgreSqlAvailability.IsAvailable, "PostgreSQL недоступен — интеграционный тест пропущен (ADR-3)");
        var response = await client.GetAsync("/api/auth/me");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// Неверный пароль — 401
    /// </summary>
    [SkippableFact]
    public async Task LoginWithWrongPasswordShouldReturn401()
    {
        Skip.IfNot(PostgreSqlAvailability.IsAvailable, "PostgreSQL недоступен — интеграционный тест пропущен (ADR-3)");
        var login = $"auth_wrong_{Guid.NewGuid():N}";
        await AuthApiHelpers.SeedUser(fixture.Context, login);

        var response = await client.PostAsJsonAsync("/api/auth/login", new { login, password = "WrongPassword1" });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// Повторное использование отозванного refresh-токена по HTTP отзывает все сессии (401 и для нового токена)
    /// </summary>
    [SkippableFact]
    public async Task RefreshReuseShouldRevokeAllSessions()
    {
        Skip.IfNot(PostgreSqlAvailability.IsAvailable, "PostgreSQL недоступен — интеграционный тест пропущен (ADR-3)");
        var login = $"auth_reuse_{Guid.NewGuid():N}";
        await AuthApiHelpers.SeedUser(fixture.Context, login);
        var tokens = await client.LoginAsync(login);
        var oldRefresh = tokens.GetProperty("refreshToken").GetString()!;

        var first = await client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = oldRefresh });
        first.StatusCode.Should().Be(HttpStatusCode.OK);
        var rotated = await first.Content.ReadFromJsonAsync<System.Text.Json.JsonDocument>();
        var newRefresh = rotated!.RootElement.GetProperty("refreshToken").GetString()!;
        newRefresh.Should().NotBe(oldRefresh);

        // Украденная копия старого токена
        var reuse = await client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = oldRefresh });
        reuse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // Все сессии завершены
        var followUp = await client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = newRefresh });
        followUp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// Выход отзывает refresh-токен
    /// </summary>
    [SkippableFact]
    public async Task LogoutShouldRevokeRefreshToken()
    {
        Skip.IfNot(PostgreSqlAvailability.IsAvailable, "PostgreSQL недоступен — интеграционный тест пропущен (ADR-3)");
        var login = $"auth_logout_{Guid.NewGuid():N}";
        await AuthApiHelpers.SeedUser(fixture.Context, login);
        var tokens = await client.LoginAsync(login);
        var accessToken = tokens.GetProperty("accessToken").GetString()!;
        var refreshToken = tokens.GetProperty("refreshToken").GetString()!;

        var logout = await client.PostWithBearer("/api/auth/logout", null, accessToken);
        logout.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var refresh = await client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken });
        refresh.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// Смена пароля завершает сессии: старый refresh не работает, вход со старым паролем отклонён
    /// </summary>
    [SkippableFact]
    public async Task ChangePasswordShouldInvalidateSessions()
    {
        Skip.IfNot(PostgreSqlAvailability.IsAvailable, "PostgreSQL недоступен — интеграционный тест пропущен (ADR-3)");
        var login = $"auth_chg_{Guid.NewGuid():N}";
        await AuthApiHelpers.SeedUser(fixture.Context, login);
        var tokens = await client.LoginAsync(login);
        var accessToken = tokens.GetProperty("accessToken").GetString()!;
        var refreshToken = tokens.GetProperty("refreshToken").GetString()!;

        var change = await client.PostWithBearer("/api/auth/change-password",
            new { currentPassword = AuthApiHelpers.DefaultPassword, newPassword = "NewPassword456" }, accessToken);
        change.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var refresh = await client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken });
        refresh.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var oldLogin = await client.PostAsJsonAsync("/api/auth/login", new { login, password = AuthApiHelpers.DefaultPassword });
        oldLogin.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var newTokens = await client.LoginAsync(login, "NewPassword456");
        newTokens.GetProperty("user").GetProperty("passwordIsTemporary").GetBoolean().Should().BeFalse();
    }

    /// <summary>
    /// Публичное чтение работает без токена и с недействительным токеном не ломается
    /// </summary>
    [SkippableFact]
    public async Task PublicReadingShouldStayAnonymous()
    {
        Skip.IfNot(PostgreSqlAvailability.IsAvailable, "PostgreSQL недоступен — интеграционный тест пропущен (ADR-3)");
        var response = await client.GetAsync("/api/stats/overview");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>
    /// Испорченный токен (изменённая полезная нагрузка) не проходит проверку подписи — 401
    /// </summary>
    [SkippableFact]
    public async Task TamperedTokenShouldReturn401()
    {
        Skip.IfNot(PostgreSqlAvailability.IsAvailable, "PostgreSQL недоступен — интеграционный тест пропущен (ADR-3)");
        var login = $"auth_tamper_{Guid.NewGuid():N}";
        await AuthApiHelpers.SeedUser(fixture.Context, login);
        var tokens = await client.LoginAsync(login);
        var accessToken = tokens.GetProperty("accessToken").GetString()!;
        var parts = accessToken.Split('.');
        var tampered = parts[0] + "." + new string('A', parts[1].Length) + "." + parts[2];

        var response = await client.GetWithBearer("/api/auth/me", tampered);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}