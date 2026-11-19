namespace DumaLens.Api.Models.Auth;

/// <summary>
/// Запрос обновления пары токенов по refresh-токену
/// </summary>
public class RefreshRequestApiModel
{
    /// <summary>
    /// Refresh-токен, выпущенный при входе или предыдущем обновлении
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;
}