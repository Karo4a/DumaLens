using DumaLens.Entities.Enums;

namespace DumaLens.Repositories.Contracts.Models;

/// <summary>
/// Модель пользователя для запроса из базы данных (сервисный слой, включая данные проверки пароля)
/// </summary>
public class UserDbModel
{
    /// <summary>
    /// Идентификатор
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Логин
    /// </summary>
    public string Login { get; set; } = string.Empty;

    /// <summary>
    /// Электронная почта
    /// </summary>
    public string Email { get; set; } = string.Empty;

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
    /// Роль пользователя
    /// </summary>
    public Role Role { get; set; }

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
    /// Дата и время последней неудачной попытки входа
    /// </summary>
    public DateTimeOffset? LastFailedLoginAt { get; set; }

    /// <summary>
    /// Заметка администратора
    /// </summary>
    public string? Note { get; set; }

    /// <summary>
    /// Дата и время создания
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }
}