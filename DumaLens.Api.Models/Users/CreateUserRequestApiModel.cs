namespace DumaLens.Api.Models.Users;

/// <summary>
/// Запрос создания пользователя (Администратор); пароль всегда временный
/// </summary>
public class CreateUserRequestApiModel
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
    /// Роль (User, Moderator, Administrator)
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Временный пароль (не менее 8 символов); смена обязательна при первом входе
    /// </summary>
    public string Password { get; set; } = string.Empty;
}