namespace DumaLens.Api.Models.Auth;

/// <summary>
/// Результат аутентификации: пара токенов и профиль пользователя
/// </summary>
public class AuthResponseApiModel
{
    /// <summary>
    /// Access-токен (JWT, срок жизни короткий)
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Refresh-токен для обновления пары (ротация при каждом обновлении)
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// Профиль пользователя
    /// </summary>
    public UserProfileApiModel User { get; set; } = new UserProfileApiModel();
}