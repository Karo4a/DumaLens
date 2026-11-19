using DumaLens.Entities.Contracts;

namespace DumaLens.Entities;

/// <summary>
/// Пользователь
/// </summary>
/// <remarks>
/// Своя схема вместо ASP.NET Core Identity (ADR-11); одна роль на пользователя (07-security.md).
/// Поля *LowerCase заполняются сервисом при записи и используются уникальными индексами.
/// </remarks>
public class User : DbBaseEntity
{
    /// <summary>
    /// Логин
    /// </summary>
    public string Login { get; set; } = string.Empty;

    /// <summary>
    /// Логин в нижнем регистре (для уникального индекса)
    /// </summary>
    public string LoginLowerCase { get; set; } = string.Empty;

    /// <summary>
    /// Электронная почта
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Электронная почта в нижнем регистре (для уникального индекса)
    /// </summary>
    public string EmailLowerCase { get; set; } = string.Empty;

    /// <summary>
    /// Отображаемое имя
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Хэш пароля (PBKDF2-SHA256, base64)
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// Соль пароля (base64)
    /// </summary>
    public string PasswordSalt { get; set; } = string.Empty;

    /// <summary>
    /// Пароль временный (требуется смена при первом входе)
    /// </summary>
    public bool PasswordIsTemporary { get; set; }

    /// <summary>
    /// Метка безопасности для отзыва сессий
    /// </summary>
    public Guid SecurityStamp { get; set; }

    /// <summary>
    /// Идентификатор роли
    /// </summary>
    public Guid RoleId { get; set; }

    /// <summary>
    /// Пользователь заблокирован
    /// </summary>
    public bool Blocked { get; set; }

    /// <summary>
    /// Счётчик неудачных попыток входа
    /// </summary>
    public int LoginAttemptFailedCount { get; set; }

    /// <summary>
    /// Дата и время последней неудачной попытки входа (окно блокировки, 07-security.md §2.2)
    /// </summary>
    public DateTimeOffset? LastFailedLoginAt { get; set; }

    /// <summary>
    /// Заметка администратора
    /// </summary>
    public string? Note { get; set; }

    /// <summary>
    /// Роль пользователя
    /// </summary>
    public UserRole Role { get; set; } = null!;
}