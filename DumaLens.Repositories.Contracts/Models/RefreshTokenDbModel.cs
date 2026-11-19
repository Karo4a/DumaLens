namespace DumaLens.Repositories.Contracts.Models;

/// <summary>
/// Модель refresh-токена для запроса из базы данных
/// </summary>
public class RefreshTokenDbModel
{
    /// <summary>
    /// Идентификатор
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Идентификатор пользователя
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Хэш refresh-токена (base64)
    /// </summary>
    public string TokenHash { get; set; } = string.Empty;

    /// <summary>
    /// Метка безопасности пользователя на момент выпуска токена
    /// </summary>
    public Guid SecurityStamp { get; set; }

    /// <summary>
    /// Дата и время истечения токена
    /// </summary>
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>
    /// Дата и время выпуска токена
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Дата и время отзыва токена (null, если токен активен)
    /// </summary>
    public DateTimeOffset? RevokedAt { get; set; }
}