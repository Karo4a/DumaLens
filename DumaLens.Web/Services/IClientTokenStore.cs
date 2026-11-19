namespace DumaLens.Web.Services;

/// <summary>
/// Хранилище токенов клиента в ProtectedLocalStorage браузера
/// </summary>
public interface IClientTokenStore
{
    /// <summary>
    /// Возвращает access-токен или null (в том числе при пререндере, когда JS-интероп недоступен)
    /// </summary>
    Task<string?> GetAccessTokenAsync();

    /// <summary>
    /// Возвращает refresh-токен или null
    /// </summary>
    Task<string?> GetRefreshTokenAsync();

    /// <summary>
    /// Сохраняет пару токенов
    /// </summary>
    Task SetTokensAsync(string accessToken, string refreshToken);

    /// <summary>
    /// Удаляет оба токена
    /// </summary>
    Task ClearAsync();
}