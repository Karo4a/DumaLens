using DumaLens.Services.Contracts;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace DumaLens.Api.Infrastructure.Authorization;

/// <summary>
/// Обработчик требования минимальной роли: проверяет клейм role по иерархии (без Identity, ADR-11)
/// </summary>
/// <remarks>
/// Administrator проходит требования Moderator; Пользователь не проходит административные.
/// </remarks>
public class MinimumRoleHandler : AuthorizationHandler<MinimumRoleRequirement>
{
    /// <inheritdoc cref="AuthorizationHandler{TRequirement}.HandleRequirementAsync"/>
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, MinimumRoleRequirement requirement)
    {
        var roleToken = context.User.FindFirstValue(ClaimTypes.Role);
        var role = RoleHelper.Parse(roleToken);
        if (role.HasValue && RoleHelper.HasMinimumRole(role.Value, requirement.MinimumRole))
        {
            context.Succeed(requirement);
        }
        return Task.CompletedTask;
    }
}