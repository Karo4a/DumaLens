using DumaLens.Api.Models.Auth;
using DumaLens.Web.Services;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace DumaLens.Web.Infrastructure;

/// <summary>
/// Добавляет Bearer-токен к запросам и автоматически обновляет пару токенов при 401 (один повтор)
/// </summary>
/// <remarks>
/// Обновление выполняется отдельным HttpClient без этого обработчика (без рекурсии) и защищено
/// семафором: параллельные запросы цепи не запускают гонку обновлений (повторное использование
/// refresh-токена отозвало бы все сессии). Если параллельный запрос уже обновил пару, повтор
/// использует свежий access-токен без обращения к Api.
/// </remarks>
public class AuthDelegatingHandler : DelegatingHandler
{
    private const string ApiClientName = "DumaLensApi";

    private readonly IClientTokenStore tokenStore;
    private readonly IHttpClientFactory clientFactory;
    private readonly SemaphoreSlim refreshLock = new(1, 1);

    /// <summary>
    /// Конструктор
    /// </summary>
    public AuthDelegatingHandler(IClientTokenStore tokenStore, IHttpClientFactory clientFactory)
    {
        this.tokenStore = tokenStore;
        this.clientFactory = clientFactory;
    }

    /// <inheritdoc cref="DelegatingHandler.SendAsync"/>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var accessToken = await tokenStore.GetAccessTokenAsync();
        SetBearer(request, accessToken);
        var response = await base.SendAsync(request, cancellationToken);
        if (response.StatusCode != HttpStatusCode.Unauthorized)
        {
            return response;
        }
        var newToken = await TryRefreshAsync(accessToken, cancellationToken);
        if (string.IsNullOrEmpty(newToken))
        {
            return response;
        }
        response.Dispose();
        var retry = await CloneRequestAsync(request);
        SetBearer(retry, newToken);
        return await base.SendAsync(retry, cancellationToken);
    }

    private static void SetBearer(HttpRequestMessage request, string? accessToken)
    {
        if (!string.IsNullOrEmpty(accessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }
    }

    private async Task<string?> TryRefreshAsync(string? tokenAtSend, CancellationToken cancellationToken)
    {
        await refreshLock.WaitAsync(cancellationToken);
        try
        {
            var current = await tokenStore.GetAccessTokenAsync();
            if (!string.IsNullOrEmpty(current) && current != tokenAtSend)
            {
                // Параллельный запрос уже обновил пару
                return current;
            }
            var refreshToken = await tokenStore.GetRefreshTokenAsync();
            if (string.IsNullOrEmpty(refreshToken))
            {
                await tokenStore.ClearAsync();
                return null;
            }
            using var client = clientFactory.CreateClient(ApiClientName);
            using var refreshResponse = await client.PostAsJsonAsync(
                "api/auth/refresh",
                new RefreshRequestApiModel { RefreshToken = refreshToken },
                cancellationToken);
            if (!refreshResponse.IsSuccessStatusCode)
            {
                await tokenStore.ClearAsync();
                return null;
            }
            var payload = await refreshResponse.Content.ReadFromJsonAsync<AuthResponseApiModel>(cancellationToken: cancellationToken);
            if (payload == null || string.IsNullOrEmpty(payload.AccessToken) || string.IsNullOrEmpty(payload.RefreshToken))
            {
                await tokenStore.ClearAsync();
                return null;
            }
            await tokenStore.SetTokensAsync(payload.AccessToken, payload.RefreshToken);
            return payload.AccessToken;
        }
        catch (HttpRequestException)
        {
            return null;
        }
        finally
        {
            refreshLock.Release();
        }
    }

    private static async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage request)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri);
        if (request.Content != null)
        {
            var bytes = await request.Content.ReadAsByteArrayAsync();
            clone.Content = new ByteArrayContent(bytes);
            foreach (var header in request.Content.Headers)
            {
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }
        foreach (var header in request.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }
        clone.Version = request.Version;
        clone.VersionPolicy = request.VersionPolicy;
        foreach (var option in request.Options)
        {
            ((IDictionary<string, object?>)clone.Options).Add(option.Key, option.Value);
        }
        return clone;
    }
}