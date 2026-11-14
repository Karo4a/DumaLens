using DumaLens.Context.Tests;
using DumaLens.Entities.Enums;
using DumaLens.Repositories.Contracts.ReadRepositories;
using DumaLens.Repositories.ReadRepositories;
using FluentAssertions;
using Xunit;

namespace DumaLens.Repositories.Tests;

/// <summary>
/// Тесты <see cref="UserRoleReadRepository"/> на базе в памяти
/// </summary>
public class UserRoleReadRepositoryTests : DumaLensContextInMemory
{
    private readonly IUserRoleReadRepository repository;

    /// <summary>
    /// Конструктор
    /// </summary>
    public UserRoleReadRepositoryTests()
    {
        repository = new UserRoleReadRepository(Context);
    }

    /// <summary>
    /// GetByRole должен возвращать роль по значению перечисления
    /// </summary>
    [Fact]
    public async Task GetByRoleShouldReturnRole()
    {
        // Arrange
        var role = await PrepareUserRole(Role.Moderator);

        // Act
        var result = await repository.GetByRole(Role.Moderator, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(role.Id);
        result.Role.Should().Be(Role.Moderator);
    }

    /// <summary>
    /// GetById должен возвращать null для мягко удалённой роли
    /// </summary>
    [Fact]
    public async Task GetByIdShouldNotReturnDeletedRole()
    {
        // Arrange
        var role = await PrepareUserRole(deletedAt: DateTimeOffset.UtcNow);

        // Act
        var result = await repository.GetById(role.Id, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// GetAll должен возвращать только неудалённые роли, отсортированные по значению
    /// </summary>
    [Fact]
    public async Task GetAllShouldReturnActiveRolesOrderedByRole()
    {
        // Arrange
        await PrepareUserRole(Role.Administrator);
        await PrepareUserRole(Role.User);
        await PrepareUserRole(Role.Moderator, deletedAt: DateTimeOffset.UtcNow);

        // Act
        var result = await repository.GetAll(CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result.Select(x => x.Role).Should().Equal(Role.User, Role.Administrator);
    }
}