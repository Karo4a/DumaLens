namespace DumaLens.Services.Contracts.Models;

/// <summary>
/// Профиль пользователя
/// </summary>
public class UserProfileModel
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
    /// Роль (строковый токен: User, Moderator, Administrator)
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Пароль временный (требуется смена при первом входе)
    /// </summary>
    public bool PasswordIsTemporary { get; set; }
}