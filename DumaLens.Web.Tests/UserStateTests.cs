using DumaLens.Web.Services;
using FluentAssertions;
using Xunit;

namespace DumaLens.Web.Tests;

/// <summary>
/// Тесты состояния пользователя цепи (Э9): заполнение из профиля, сброс, иерархия ролей, события
/// </summary>
public class UserStateTests
{
    /// <summary>
    /// SetFrom заполняет профиль, помечает инициализацию и поднимает событие
    /// </summary>
    [Fact]
    public void SetFromShouldPopulateStateAndNotify()
    {
        var state = new UserState();
        var notified = 0;
        state.OnChange += () => notified++;
        var id = Guid.NewGuid();

        state.SetFrom(id, "admin", "Администратор", "admin@example.com", "Administrator", passwordIsTemporary: true);

        state.IsAuthenticated.Should().BeTrue();
        state.IsInitialized.Should().BeTrue();
        state.Id.Should().Be(id);
        state.Login.Should().Be("admin");
        state.Name.Should().Be("Администратор");
        state.Email.Should().Be("admin@example.com");
        state.Role.Should().Be("Administrator");
        state.PasswordIsTemporary.Should().BeTrue();
        state.IsAdministrator.Should().BeTrue();
        state.IsModerator.Should().BeTrue();
        notified.Should().Be(1);
    }

    /// <summary>
    /// Clear сбрасывает профиль, сохраняет признак инициализации и поднимает событие
    /// </summary>
    [Fact]
    public void ClearShouldResetStateAndNotify()
    {
        var state = new UserState();
        state.SetFrom(Guid.NewGuid(), "reader", "Читатель", "", "User", passwordIsTemporary: false);
        var notified = 0;
        state.OnChange += () => notified++;

        state.Clear();

        state.IsAuthenticated.Should().BeFalse();
        state.IsInitialized.Should().BeTrue();
        state.Id.Should().BeNull();
        state.Login.Should().BeNull();
        state.Role.Should().BeNull();
        state.IsAdministrator.Should().BeFalse();
        state.IsModerator.Should().BeFalse();
        notified.Should().Be(1);
    }

    /// <summary>
    /// Иерархия: Moderator — модератор, но не администратор; User — ни то ни другое
    /// </summary>
    [Fact]
    public void RoleHierarchyShouldMatchSecuritySpec()
    {
        var moderator = new UserState();
        moderator.SetFrom(Guid.NewGuid(), "mod", "Модератор", "", "Moderator", passwordIsTemporary: false);
        moderator.IsModerator.Should().BeTrue();
        moderator.IsAdministrator.Should().BeFalse();

        var user = new UserState();
        user.SetFrom(Guid.NewGuid(), "usr", "Пользователь", "", "User", passwordIsTemporary: false);
        user.IsModerator.Should().BeFalse();
        user.IsAdministrator.Should().BeFalse();
    }

    /// <summary>
    /// Новое состояние не инициализировано и анонимно
    /// </summary>
    [Fact]
    public void NewStateShouldBeAnonymousAndUninitialized()
    {
        var state = new UserState();
        state.IsAuthenticated.Should().BeFalse();
        state.IsInitialized.Should().BeFalse();
    }
}