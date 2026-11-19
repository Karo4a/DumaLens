using DumaLens.Entities.Enums;
using DumaLens.Services.Contracts;
using FluentAssertions;
using Xunit;

namespace DumaLens.Services.Tests;

/// <summary>
/// Тесты иерархии ролей User ⊂ Moderator ⊂ Administrator и разбора токенов (07-security.md §1)
/// </summary>
public class RoleHelperTests
{
    /// <summary>
    /// Administrator проходит требования всех ролей
    /// </summary>
    [Theory]
    [InlineData(Role.User)]
    [InlineData(Role.Moderator)]
    [InlineData(Role.Administrator)]
    public void AdministratorShouldSatisfyEveryMinimum(Role minimum)
        => RoleHelper.HasMinimumRole(Role.Administrator, minimum).Should().BeTrue();

    /// <summary>
    /// Moderator проходит User и Moderator, но не Administrator
    /// </summary>
    [Fact]
    public void ModeratorShouldSatisfyUserAndModeratorOnly()
    {
        RoleHelper.HasMinimumRole(Role.Moderator, Role.User).Should().BeTrue();
        RoleHelper.HasMinimumRole(Role.Moderator, Role.Moderator).Should().BeTrue();
        RoleHelper.HasMinimumRole(Role.Moderator, Role.Administrator).Should().BeFalse();
    }

    /// <summary>
    /// User проходит только User
    /// </summary>
    [Fact]
    public void UserShouldSatisfyUserOnly()
    {
        RoleHelper.HasMinimumRole(Role.User, Role.User).Should().BeTrue();
        RoleHelper.HasMinimumRole(Role.User, Role.Moderator).Should().BeFalse();
        RoleHelper.HasMinimumRole(Role.User, Role.Administrator).Should().BeFalse();
    }

    /// <summary>
    /// Разбор токенов ролей; неизвестный токен — null
    /// </summary>
    [Fact]
    public void ParseShouldRecognizeKnownTokensOnly()
    {
        RoleHelper.Parse("User").Should().Be(Role.User);
        RoleHelper.Parse("Moderator").Should().Be(Role.Moderator);
        RoleHelper.Parse("Administrator").Should().Be(Role.Administrator);
        RoleHelper.Parse("admin").Should().BeNull();
        RoleHelper.Parse("").Should().BeNull();
        RoleHelper.Parse(null).Should().BeNull();
    }
}