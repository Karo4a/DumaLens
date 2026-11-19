namespace DumaLens.Services.Contracts.Models;

/// <summary>
/// Запрос создания пользователя (Администратор)
/// </summary>
/// <remarks>
/// Пароль всегда временный: при первом входе требуется смена (07-security.md §1).
/// </remarks>
public class CreateUserModel
{
    /// <summary>
    /// Логин
    /// </summary>
    public string Login { get; set; } = string.Empty;

    /// <summary>
    /// Электронная почта (необязательна)
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Отображаемое имя
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Роль (строковый токен: User, Moderator, Administrator)
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Временный пароль
    /// </summary>
    public string Password { get; set; } = string.Empty;
}