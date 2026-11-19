namespace DumaLens.Api.Models.Auth;

/// <summary>
/// Профиль пользователя
/// </summary>
public class UserProfileApiModel
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
    /// Отображаемое имя
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Электронная почта
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Роль (User, Moderator, Administrator)
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Пароль временный: требуется смена при первом входе
    /// </summary>
    public bool PasswordIsTemporary { get; set; }
}