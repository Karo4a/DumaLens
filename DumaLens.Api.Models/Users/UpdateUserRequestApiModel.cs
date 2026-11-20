namespace DumaLens.Api.Models.Users;

/// <summary>
/// Запрос обновления пользователя (Администратор): профиль, роль, блокировка
/// </summary>
public class UpdateUserRequestApiModel
{
    /// <summary>
    /// Отображаемое имя
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Электронная почта (необязательна)
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Роль (User, Moderator, Administrator)
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Пользователь заблокирован (обязательное поле: пропуск в запросе недопустим)
    /// </summary>
    public bool? Blocked { get; set; }

    /// <summary>
    /// Заметка администратора
    /// </summary>
    public string? Note { get; set; }
}