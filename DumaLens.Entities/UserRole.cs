using DumaLens.Entities.Contracts;
using DumaLens.Entities.Enums;

namespace DumaLens.Entities;

/// <summary>
/// Роль пользователя (справочник Roles)
/// </summary>
/// <remarks>
/// Три роли с фиксированными идентификаторами — seed <c>UserRolesDefault</c> (07-security.md, §1).
/// </remarks>
public class UserRole : DbBaseEntity
{
    /// <summary>
    /// Роль (хранится строкой в колонке Name)
    /// </summary>
    public Role Role { get; set; }

    /// <summary>
    /// Описание роли
    /// </summary>
    public string Description { get; set; } = string.Empty;
}