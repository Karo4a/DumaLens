namespace DumaLens.Services.Contracts.Models;

/// <summary>
/// Результат аутентификации: пара токенов и профиль пользователя
/// </summary>
public class AuthResultModel
{
    /// <summary>
    /// Access-токен (JWT)
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Refresh-токен (исходная строка; в базе хранится только хэш)
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// Профиль пользователя
    /// </summary>
    public UserProfileModel User { get; set; } = new UserProfileModel();
}