using DumaLens.Entities.Enums;

namespace DumaLens.Services.Contracts;

/// <summary>
/// Утилита ролей: иерархия User ⊂ Moderator ⊂ Administrator и разбор строковых токенов (07-security.md §1)
/// </summary>
public static class RoleHelper
{
    /// <summary>
    /// Проверяет, что роль удовлетворяет минимальному уровню иерархии
    /// </summary>
    /// <param name="role">Роль пользователя</param>
    /// <param name="minimum">Минимально требуемая роль</param>
    public static bool HasMinimumRole(Role role, Role minimum)
        => (int)role >= (int)minimum;

    /// <summary>
    /// Разбирает строковый токен роли; неизвестный токен — null
    /// </summary>
    /// <param name="token">Токен роли (User, Moderator, Administrator)</param>
    public static Role? Parse(string? token)
        => token switch
        {
            "User" => Role.User,
            "Moderator" => Role.Moderator,
            "Administrator" => Role.Administrator,
            _ => null,
        };
}