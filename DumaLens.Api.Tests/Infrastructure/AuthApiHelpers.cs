using DumaLens.Context;
using DumaLens.Entities;
using DumaLens.Entities.Default;
using DumaLens.Entities.Enums;
using DumaLens.Services.Auth;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace DumaLens.Api.Tests.Infrastructure;

/// <summary>
/// Хелперы интеграционных тестов аутентификации (Э9): посев пользователей и вход по HTTP
/// </summary>
internal static class AuthApiHelpers
{
    /// <summary>
    /// Пароль тестовых пользователей по умолчанию
    /// </summary>
    internal const string DefaultPassword = "Password123";

    /// <summary>
    /// Создаёт пользователя напрямую в базе тестового хоста
    /// </summary>
    internal static async Task<User> SeedUser(DumaLensContext context, string login, Role role = Role.User, string password = DefaultPassword, bool blocked = false, bool temporary = false)
    {
        var salt = SecurityHelper.GenerateSalt();
        var user = new User
        {
            Login = login,
            LoginLowerCase = login.ToLowerInvariant(),
            Email = string.Empty,
            EmailLowerCase = string.Empty,
            Name = login,
            PasswordSalt = salt,
            PasswordHash = SecurityHelper.HashPassword(password, salt),
            PasswordIsTemporary = temporary,
            SecurityStamp = Guid.NewGuid(),
            RoleId = UserRolesDefault.DefaultRoleToGuid(role),
            Blocked = blocked,
        };
        context.Set<User>().Add(user);
        await context.SaveChangesAsync();
        return user;
    }

    /// <summary>
    /// Выполняет вход и возвращает корень JSON-ответа (accessToken, refreshToken, user)
    /// </summary>
    internal static async Task<JsonElement> LoginAsync(this HttpClient client, string login, string password = DefaultPassword)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new { login, password });
        response.StatusCode.Should().Be(HttpStatusCode.OK, "вход {0} должен завершиться успешно", login);
        var document = await response.Content.ReadFromJsonAsync<JsonDocument>();
        return document!.RootElement;
    }

    /// <summary>
    /// Выполняет GET с Bearer-токеном
    /// </summary>
    internal static async Task<HttpResponseMessage> GetWithBearer(this HttpClient client, string url, string accessToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        return await client.SendAsync(request);
    }

    /// <summary>
    /// Выполняет POST JSON с Bearer-токеном
    /// </summary>
    internal static async Task<HttpResponseMessage> PostWithBearer(this HttpClient client, string url, object? body, string accessToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        if (body != null)
        {
            request.Content = JsonContent.Create(body);
        }
        return await client.SendAsync(request);
    }
}