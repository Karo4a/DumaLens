using DumaLens.Api.Infrastructure.Authorization;
using DumaLens.Entities.Enums;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Xunit;

namespace DumaLens.Api.Tests;

/// <summary>
/// Тесты иерархии ролей в авторизации Api (Э9, 07-security.md §3):
/// Administrator проходит требования Moderator, Moderator не проходит Administrator
/// </summary>
public class MinimumRoleHandlerTests
{
    /// <summary>
    /// Administrator удовлетворяет требованию Moderator (иерархия)
    /// </summary>
    [Fact]
    public async Task AdministratorShouldSatisfyModeratorRequirement()
    {
        var context = CreateContext("Administrator", Role.Moderator);
        await new MinimumRoleHandler().HandleAsync(context);
        context.HasSucceeded.Should().BeTrue();
    }

    /// <summary>
    /// Moderator удовлетворяет требованию Moderator, но не Administrator
    /// </summary>
    [Fact]
    public async Task ModeratorShouldSatisfyModeratorOnly()
    {
        var moderator = CreateContext("Moderator", Role.Moderator);
        await new MinimumRoleHandler().HandleAsync(moderator);
        moderator.HasSucceeded.Should().BeTrue();

        var administrator = CreateContext("Moderator", Role.Administrator);
        await new MinimumRoleHandler().HandleAsync(administrator);
        administrator.HasSucceeded.Should().BeFalse();
    }

    /// <summary>
    /// User не проходит требования Moderator и Administrator
    /// </summary>
    [Fact]
    public async Task UserShouldNotSatisfyElevatedRequirements()
    {
        var moderator = CreateContext("User", Role.Moderator);
        await new MinimumRoleHandler().HandleAsync(moderator);
        moderator.HasSucceeded.Should().BeFalse();

        var administrator = CreateContext("User", Role.Administrator);
        await new MinimumRoleHandler().HandleAsync(administrator);
        administrator.HasSucceeded.Should().BeFalse();
    }

    /// <summary>
    /// Без клейма роли или с неизвестным клеймом требование не выполняется
    /// </summary>
    [Fact]
    public async Task MissingOrUnknownRoleShouldNotSatisfyRequirement()
    {
        var missing = CreateContext(null, Role.User);
        await new MinimumRoleHandler().HandleAsync(missing);
        missing.HasSucceeded.Should().BeFalse();

        var unknown = CreateContext("Superuser", Role.User);
        await new MinimumRoleHandler().HandleAsync(unknown);
        unknown.HasSucceeded.Should().BeFalse();
    }

    private static AuthorizationHandlerContext CreateContext(string? role, Role minimum)
    {
        var claims = new List<Claim>();
        if (role != null)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "Test"));
        return new AuthorizationHandlerContext([new MinimumRoleRequirement(minimum)], principal, resource: null);
    }
}