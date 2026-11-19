using DumaLens.Entities.Enums;
using Microsoft.AspNetCore.Authorization;

namespace DumaLens.Api.Infrastructure.Authorization;

/// <summary>
/// Требование минимальной роли по иерархии User ⊂ Moderator ⊂ Administrator (07-security.md §3)
/// </summary>
public class MinimumRoleRequirement : IAuthorizationRequirement
{
    /// <summary>
    /// Конструктор
    /// </summary>
    /// <param name="minimumRole">Минимально требуемая роль</param>
    public MinimumRoleRequirement(Role minimumRole)
    {
        MinimumRole = minimumRole;
    }

    /// <summary>
    /// Минимально требуемая роль
    /// </summary>
    public Role MinimumRole { get; }
}