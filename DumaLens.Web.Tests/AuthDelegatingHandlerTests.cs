using DumaLens.Web.Infrastructure;
using DumaLens.Web.Services;
using FluentAssertions;
using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;

namespace DumaLens.Web.Tests;

/// <summary>
/// Тесты обработчика аутентификации HttpClient (Э9): Bearer, авто-refresh при 401 с одним повтором,
/// единый refresh для параллельных запросов, очистка токенов при неудачном обновлении
/// </summary>
public class AuthDelegatingHandlerTests
{
    /// <summary>
    /// Запрос получает Bearer-токен из хранилища
    /// </summary>
    [Fact]
    public async Task RequestShouldCarryBearerToken()
    {
        var store = new FakeTokenStore { Access = "token1" };
        var inner = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        using var client = CreateClient(store, inner, new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));

        var response = await client.GetAsync("http://localhost/api/stats/overview");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        inner.Requests.Should().ContainSingle();
        inner.Requests[0].Headers.Authorization!.Scheme.Should().Be("Bearer");
        inner.Requests[0].Headers.Authorization!.Parameter.Should().Be("token1");
    }

    /// <summary>
    /// При 401 пара обновляется по refresh-токену, запрос повторяется с новым токеном
    /// </summary>
    [Fact]
    public async Task UnauthorizedShouldRefreshAndRetryOnce()
    {
        var store = new FakeTokenStore { Access = "old", Refresh = "refresh1" };
        var inner = new StubHandler(request =>
            request.Headers.Authorization?.Parameter == "new"
                ? new HttpResponseMessage(HttpStatusCode.OK)
                : new HttpResponseMessage(HttpStatusCode.Unauthorized));
        var refreshCalls = 0;
        var refreshHandler = new StubHandler(request =>
        {
            refreshCalls++;
            request.RequestUri!.AbsolutePath.Should().Be("/api/auth/refresh");
            var body = request.Content!.ReadAsStringAsync().Result;
            body.Should().Contain("refresh1");
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(new { accessToken = "new", refreshToken = "refresh2" }),
                    Encoding.UTF8,
                    "application/json"),
            };
        });
        using var client = CreateClient(store, inner, refreshHandler);

        var response = await client.GetAsync("http://localhost/api/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        refreshCalls.Should().Be(1);
        inner.Requests.Should().HaveCount(2);
        inner.Requests[1].Headers.Authorization!.Parameter.Should().Be("new");
        store.Access.Should().Be("new");
        store.Refresh.Should().Be("refresh2");
    }

    /// <summary>
    /// Неудачное обновление очищает токены и возвращает исходный 401
    /// </summary>
    [Fact]
    public async Task FailedRefreshShouldClearTokensAndReturn401()
    {
        var store = new FakeTokenStore { Access = "old", Refresh = "revoked" };
        var inner = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized));
        var refreshHandler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized));
        using var client = CreateClient(store, inner, refreshHandler);

        var response = await client.GetAsync("http://localhost/api/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        store.Access.Should().BeNull();
        store.Refresh.Should().BeNull();
        inner.Requests.Should().ContainSingle("повтор без нового токена не выполняется");
    }

    /// <summary>
    /// Без refresh-токена обновление не выполняется
    /// </summary>
    [Fact]
    public async Task WithoutRefreshTokenShouldNotCallRefresh()
    {
        var store = new FakeTokenStore { Access = "old", Refresh = null };
        var inner = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized));
        var refreshHandler = new StubHandler(_ => throw new InvalidOperationException("refresh не должен вызываться"));
        using var client = CreateClient(store, inner, refreshHandler);

        var response = await client.GetAsync("http://localhost/api/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        refreshHandler.Requests.Should().BeEmpty();
    }

    /// <summary>
    /// Параллельные запросы с одним просроченным токеном выполняют ровно одно обновление
    /// </summary>
    [Fact]
    public async Task ParallelRequestsShouldShareSingleRefresh()
    {
        var store = new FakeTokenStore { Access = "old", Refresh = "refresh1" };
        var inner = new StubHandler(request =>
            request.Headers.Authorization?.Parameter == "new"
                ? new HttpResponseMessage(HttpStatusCode.OK)
                : new HttpResponseMessage(HttpStatusCode.Unauthorized));
        var refreshCalls = 0;
        var refreshHandler = new StubHandler(_ =>
        {
            Interlocked.Increment(ref refreshCalls);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(new { accessToken = "new", refreshToken = "refresh2" }),
                    Encoding.UTF8,
                    "application/json"),
            };
        });
        using var client = CreateClient(store, inner, refreshHandler);

        var responses = await Task.WhenAll(
            client.GetAsync("http://localhost/api/auth/me"),
            client.GetAsync("http://localhost/api/users?page=1&pageSize=20"),
            client.GetAsync("http://localhost/api/stats/overview"));

        responses.Should().OnlyContain(r => r.StatusCode == HttpStatusCode.OK);
        refreshCalls.Should().Be(1, "гонка обновлений отозвала бы все сессии");
    }

    private static HttpClient CreateClient(FakeTokenStore store, StubHandler inner, StubHandler refresh)
    {
        var handler = new AuthDelegatingHandler(store, new StubHttpClientFactory(refresh))
        {
            InnerHandler = inner,
        };
        return new HttpClient(handler);
    }

    private sealed class FakeTokenStore : IClientTokenStore
    {
        public string? Access { get; set; }

        public string? Refresh { get; set; }

        public Task<string?> GetAccessTokenAsync() => Task.FromResult(Access);

        public Task<string?> GetRefreshTokenAsync() => Task.FromResult(Refresh);

        public Task SetTokensAsync(string accessToken, string refreshToken)
        {
            Access = accessToken;
            Refresh = refreshToken;
            return Task.CompletedTask;
        }

        public Task ClearAsync()
        {
            Access = null;
            Refresh = null;
            return Task.CompletedTask;
        }
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> responder;

        public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            this.responder = responder;
        }

        public List<HttpRequestMessage> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return Task.FromResult(responder(request));
        }
    }

    private sealed class StubHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpMessageHandler handler;

        public StubHttpClientFactory(HttpMessageHandler handler)
        {
            this.handler = handler;
        }

        public HttpClient CreateClient(string name)
            => new HttpClient(handler, disposeHandler: false) { BaseAddress = new Uri("http://localhost/") };
    }
}