using DumaLens.Context;
using DumaLens.Importer.Tests.Infrastructure;
using DumaLens.Services.Auth;
using DumaLens.Services.Contracts;
using DumaLens.Services.Contracts.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DumaLens.Importer.Tests.Integration;

/// <summary>
/// Интеграционный тест обвязки CLI create-admin (Э9): DI импортёра резолвит сервисы auth,
/// администратор создаётся с временным паролем и входит в систему
/// </summary>
public sealed class CreateAdminWiringTests : IAsyncLifetime
{
    private readonly DatabaseFixture fixture = new();

    /// <inheritdoc cref="IAsyncLifetime.InitializeAsync"/>
    public Task InitializeAsync()
        => fixture.InitializeAsync();

    /// <inheritdoc cref="IAsyncLifetime.DisposeAsync"/>
    public Task DisposeAsync()
        => fixture.DisposeAsync();

    /// <summary>
    /// Создание администратора через контейнер импортёра: роль, временный пароль, хэш в базе.
    /// Вход и выпуск токенов проверяются в Services.Tests — CLI create-admin токенов не выпускает
    /// </summary>
    [SkippableFact]
    public async Task CreateAdminViaImporterHostShouldWork()
    {
        Skip.If(fixture.SkipReason != null, fixture.SkipReason);
        await using var provider = ImporterHost.BuildServiceProvider(fixture.ConnectionString, logFilePath: null);
        await using var scope = provider.CreateAsyncScope();
        var userService = scope.ServiceProvider.GetRequiredService<IUserService>();

        var password = SecurityHelper.GenerateTemporaryPassword();
        var user = await userService.Create(new CreateUserModel
        {
            Login = "admin",
            Name = "Администратор",
            Role = "Administrator",
            Password = password,
        }, CancellationToken.None);

        user.Role.Should().Be("Administrator");
        user.PasswordIsTemporary.Should().BeTrue();
        var context = scope.ServiceProvider.GetRequiredService<DumaLensContext>();
        var stored = await context.Set<DumaLens.Entities.User>().AsNoTracking().SingleAsync(x => x.Id == user.Id);
        stored.PasswordHash.Should().NotBe(password);
        SecurityHelper.VerifyPassword(password, stored.PasswordSalt, stored.PasswordHash).Should().BeTrue();
        stored.Blocked.Should().BeFalse();
    }
}