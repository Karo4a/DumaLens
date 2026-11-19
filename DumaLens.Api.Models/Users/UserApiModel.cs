namespace DumaLens.Api.Models.Users;

/// <summary>
/// Пользователь (список и результат управления)
/// </summary>
public class UserApiModel
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
    /// Роль (User, Moderator, Administrator)
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Пользователь заблокирован
    /// </summary>
    public bool Blocked { get; set; }

    /// <summary>
    /// Пароль временный: требуется смена при первом входе
    /// </summary>
    public bool PasswordIsTemporary { get; set; }

    /// <summary>
    /// Счётчик неудачных попыток входа
    /// </summary>
    public int LoginAttemptFailedCount { get; set; }

    /// <summary>
    /// Заметка администратора
    /// </summary>
    public string? Note { get; set; }

    /// <summary>
    /// Дата и время создания
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }
}