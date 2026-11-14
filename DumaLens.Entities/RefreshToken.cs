using DumaLens.Entities.Contracts;

namespace DumaLens.Entities;

/// <summary>
/// Refresh-токен сессии пользователя
/// </summary>
/// <remarks>
/// В БД хранится хэш токена; ротация отзывает старый токен (RevokedAt) и выпускает новый (ADR-13).
/// Один активный refresh на пользователя — уникальный индекс UserId с фильтром по RevokedAt (07-security.md, §2.1).
/// </remarks>
public class RefreshToken : IEntityWithId
{
    /// <inheritdoc cref="IEntityWithId.Id"/>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Идентификатор пользователя
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Хэш refresh-токена (base64)
    /// </summary>
    public string TokenHash { get; set; } = string.Empty;

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