using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace DumaLens.Web.Services;

/// <inheritdoc cref="IClientTokenStore"/>
/// <remarks>
/// ProtectedLocalStorage шифрует значения ключами DataProtection сервера.
/// Во время пререндера JS-интероп недоступен — методы чтения возвращают null, записи пропускаются.
/// </remarks>
public class ClientTokenStore : IClientTokenStore
{
    private const string AccessTokenKey = "dl_access_token";
    private const string RefreshTokenKey = "dl_refresh_token";

    private readonly ProtectedLocalStorage storage;

    /// <summary>
    /// Конструктор
    /// </summary>
    public ClientTokenStore(ProtectedLocalStorage storage)
    {
        this.storage = storage;
    }

    /// <inheritdoc cref="IClientTokenStore.GetAccessTokenAsync"/>
    public Task<string?> GetAccessTokenAsync()
        => GetAsync(AccessTokenKey);

    /// <inheritdoc cref="IClientTokenStore.GetRefreshTokenAsync"/>
    public Task<string?> GetRefreshTokenAsync()
        => GetAsync(RefreshTokenKey);

    /// <inheritdoc cref="IClientTokenStore.SetTokensAsync"/>
    public async Task SetTokensAsync(string accessToken, string refreshToken)
    {
        try
        {
            await storage.SetAsync(AccessTokenKey, accessToken);
            await storage.SetAsync(RefreshTokenKey, refreshToken);
        }
        catch (InvalidOperationException)
        {
            // Пререндер: JS-интероп недоступен
        }
    }

    /// <inheritdoc cref="IClientTokenStore.ClearAsync"/>
    public async Task ClearAsync()
    {
        try
        {
            await storage.DeleteAsync(AccessTokenKey);
            await storage.DeleteAsync(RefreshTokenKey);
        }
        catch (InvalidOperationException)
        {
            // Пререндер: JS-интероп недоступен
        }
    }

    private async Task<string?> GetAsync(string key)
    {
        try
        {
            var result = await storage.GetAsync<string>(key);
            return result.Success ? result.Value : null;
        }
        catch (InvalidOperationException)
        {
            // Пререндер: JS-интероп недоступен
            return null;
        }
    }
}