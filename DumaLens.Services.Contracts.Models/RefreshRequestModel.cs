namespace DumaLens.Services.Contracts.Models;

/// <summary>
/// Запрос обновления пары токенов по refresh-токену
/// </summary>
public class RefreshRequestModel
{
    /// <summary>
    /// Refresh-токен (случайная строка, выпущенная при входе или предыдущем обновлении)
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;
}