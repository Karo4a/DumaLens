namespace DumaLens.Entities.Enums;

/// <summary>
/// Роль пользователя
/// </summary>
/// <remarks>
/// Иерархия прав: User ⊂ Moderator ⊂ Administrator (07-security.md, §1).
/// </remarks>
public enum Role
{
    /// <summary>
    /// Пользователь — зарегистрированный читатель
    /// </summary>
    User = 0,
    /// <summary>
    /// Модератор — ведёт справочники и теги
    /// </summary>
    Moderator = 1,
    /// <summary>
    /// Администратор — управляет системой
    /// </summary>
    Administrator = 2
}