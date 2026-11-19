using DumaLens.Api.Tests.Infrastructure;
using DumaLens.Entities.Enums;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace DumaLens.Api.Tests;

/// <summary>
/// Интеграционные тесты управления пользователями (Э9, 07-security.md §3, §5):
/// 401 без токена, 403 для User и Moderator, CRUD Администратора, 409/422/404
/// </summary>
[Collection(nameof(DumaLensCollection))]
public class UsersApiTests
{
    private readonly DumaLensApiFixture fixture;
    private readonly HttpClient client;

    /// <summary>
    /// Конструктор
    /// </summary>
    public UsersApiTests(DumaLensApiFixture fixture)
    {
        this.fixture = fixture;
        client = fixture.CreateClient();
    }

    /// <summary>
    /// Эндпоинты пользователей без токена — 401
    /// </summary>
    [SkippableFact]
    public async Task UsersWithoutTokenShouldReturn401()
    {
        Skip.IfNot(PostgreSqlAvailability.IsAvailable, "PostgreSQL недоступен — интеграционный тест пропущен (ADR-3)");
        var response = await client.GetAsync("/api/users?page=1&pageSize=20");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// Роль User — 403 на административных эндпоинтах
    /// </summary>
    [SkippableFact]
    public async Task UsersWithUserRoleShouldReturn403()
    {
        Skip.IfNot(PostgreSqlAvailability.IsAvailable, "PostgreSQL недоступен — интеграционный тест пропущен (ADR-3)");
        var login = $"users_plain_{Guid.NewGuid():N}";
        await AuthApiHelpers.SeedUser(fixture.Context, login);
        var tokens = await client.LoginAsync(login);

        var response = await client.GetWithBearer("/api/users?page=1&pageSize=20", tokens.GetProperty("accessToken").GetString()!);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    /// <summary>
    /// Роль Moderator — 403 на административных эндпоинтах (иерархия: Moderator не дотягивает до Administrator)
    /// </summary>
    [SkippableFact]
    public async Task UsersWithModeratorRoleShouldReturn403()
    {
        Skip.IfNot(PostgreSqlAvailability.IsAvailable, "PostgreSQL недоступен — интеграционный тест пропущен (ADR-3)");
        var login = $"users_mod_{Guid.NewGuid():N}";
        await AuthApiHelpers.SeedUser(fixture.Context, login, Role.Moderator);
        var tokens = await client.LoginAsync(login);

        var response = await client.GetWithBearer("/api/users?page=1&pageSize=20", tokens.GetProperty("accessToken").GetString()!);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    /// <summary>
    /// Администратор: список, создание, смена роли с отзывом сессий, сброс пароля
    /// </summary>
    [SkippableFact]
    public async Task AdministratorShouldManageUsers()
    {
        Skip.IfNot(PostgreSqlAvailability.IsAvailable, "PostgreSQL недоступен — интеграционный тест пропущен (ADR-3)");
        var adminLogin = $"users_admin_{Guid.NewGuid():N}";
        await AuthApiHelpers.SeedUser(fixture.Context, adminLogin, Role.Administrator);
        var adminTokens = await client.LoginAsync(adminLogin);
        var adminToken = adminTokens.GetProperty("accessToken").GetString()!;

        // Список
        var list = await client.GetWithBearer("/api/users?page=1&pageSize=20", adminToken);
        list.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await list.Content.ReadFromJsonAsync<JsonDocument>();
        page!.RootElement.GetProperty("totalCount").GetInt32().Should().BeGreaterThan(0);

        // Создание
        var newLogin = $"users_new_{Guid.NewGuid():N}";
        var create = await client.PostWithBearer("/api/users",
            new { login = newLogin, name = "Новый пользователь", role = "User", password = "TempPassword123" }, adminToken);
        create.StatusCode.Should().Be(HttpStatusCode.OK);
        var created = await create.Content.ReadFromJsonAsync<JsonDocument>();
        var userId = created!.RootElement.GetProperty("id").GetGuid();
        created.RootElement.GetProperty("passwordIsTemporary").GetBoolean().Should().BeTrue();
        created.RootElement.GetProperty("role").GetString().Should().Be("User");

        // Вход нового пользователя и смена роли модератором администратора
        var userTokens = await client.LoginAsync(newLogin, "TempPassword123");
        var userRefresh = userTokens.GetProperty("refreshToken").GetString()!;
        var update = await client.SendAsync(new HttpRequestMessage(HttpMethod.Put, $"/api/users/{userId}")
        {
            Content = JsonContent.Create(new { name = "Новый пользователь", role = "Moderator", blocked = false }),
            Headers = { Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken) },
        });
        update.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await update.Content.ReadFromJsonAsync<JsonDocument>();
        updated!.RootElement.GetProperty("role").GetString().Should().Be("Moderator");

        // Сессия пользователя завершена сменой роли
        var refresh = await client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = userRefresh });
        refresh.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // Сброс пароля
        var reset = await client.PostWithBearer($"/api/users/{userId}/reset-password", null, adminToken);
        reset.StatusCode.Should().Be(HttpStatusCode.OK);
        var resetBody = await reset.Content.ReadFromJsonAsync<JsonDocument>();
        var temporaryPassword = resetBody!.RootElement.GetProperty("temporaryPassword").GetString()!;
        temporaryPassword.Should().NotBeNullOrEmpty();
        var oldLogin = await client.PostAsJsonAsync("/api/auth/login", new { login = newLogin, password = "TempPassword123" });
        oldLogin.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var tempLogin = await client.LoginAsync(newLogin, temporaryPassword);
        tempLogin.GetProperty("user").GetProperty("passwordIsTemporary").GetBoolean().Should().BeTrue();
    }

    /// <summary>
    /// Повторный логин — 409
    /// </summary>
    [SkippableFact]
    public async Task CreateDuplicateLoginShouldReturn409()
    {
        Skip.IfNot(PostgreSqlAvailability.IsAvailable, "PostgreSQL недоступен — интеграционный тест пропущен (ADR-3)");
        var adminLogin = $"users_dup_admin_{Guid.NewGuid():N}";
        await AuthApiHelpers.SeedUser(fixture.Context, adminLogin, Role.Administrator);
        var adminTokens = await client.LoginAsync(adminLogin);
        var adminToken = adminTokens.GetProperty("accessToken").GetString()!;
        var existing = $"users_dup_{Guid.NewGuid():N}";
        await AuthApiHelpers.SeedUser(fixture.Context, existing);

        var create = await client.PostWithBearer("/api/users",
            new { login = existing.ToUpperInvariant(), name = "Дубль", role = "User", password = "TempPassword123" }, adminToken);
        create.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    /// <summary>
    /// Неизвестная роль и короткий пароль — 422
    /// </summary>
    [SkippableFact]
    public async Task CreateInvalidDataShouldReturn422()
    {
        Skip.IfNot(PostgreSqlAvailability.IsAvailable, "PostgreSQL недоступен — интеграционный тест пропущен (ADR-3)");
        var adminLogin = $"users_inv_admin_{Guid.NewGuid():N}";
        await AuthApiHelpers.SeedUser(fixture.Context, adminLogin, Role.Administrator);
        var adminTokens = await client.LoginAsync(adminLogin);
        var adminToken = adminTokens.GetProperty("accessToken").GetString()!;

        var badRole = await client.PostWithBearer("/api/users",
            new { login = $"users_bad_{Guid.NewGuid():N}", name = "Икс", role = "Superuser", password = "TempPassword123" }, adminToken);
        badRole.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var shortPassword = await client.PostWithBearer("/api/users",
            new { login = $"users_short_{Guid.NewGuid():N}", name = "Икс", role = "User", password = "short" }, adminToken);
        shortPassword.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    /// <summary>
    /// Обновление несуществующего пользователя — 404
    /// </summary>
    [SkippableFact]
    public async Task UpdateUnknownUserShouldReturn404()
    {
        Skip.IfNot(PostgreSqlAvailability.IsAvailable, "PostgreSQL недоступен — интеграционный тест пропущен (ADR-3)");
        var adminLogin = $"users_404_admin_{Guid.NewGuid():N}";
        await AuthApiHelpers.SeedUser(fixture.Context, adminLogin, Role.Administrator);
        var adminTokens = await client.LoginAsync(adminLogin);
        var adminToken = adminTokens.GetProperty("accessToken").GetString()!;

        var update = await client.SendAsync(new HttpRequestMessage(HttpMethod.Put, $"/api/users/{Guid.NewGuid()}")
        {
            Content = JsonContent.Create(new { name = "Призрак", role = "User", blocked = false }),
            Headers = { Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken) },
        });
        update.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}