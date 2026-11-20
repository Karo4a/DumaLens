using DumaLens.Entities;
using DumaLens.Entities.Enums;
using DumaLens.Services.Auth;
using DumaLens.Services.Contracts.Exceptions;
using DumaLens.Services.Contracts.Models;
using DumaLens.Services.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DumaLens.Services.Tests;

/// <summary>
/// Интеграционные тесты управления пользователями (Э9, 07-security.md §5) на одноразовой
/// PostgreSQL-базе: создание с временным паролем, уникальность логина и почты, пагинация,
/// смена роли и блокировка с отзывом сессий, защита последнего администратора, сброс пароля
/// </summary>
public sealed class UserServiceTests : AuthTestBase
{
    /// <summary>
    /// Создание: пароль хэшируется, помечается временным, роль резолвится из справочника
    /// </summary>
    [SkippableFact]
    public async Task CreateShouldHashPasswordAndMarkTemporary()
    {
        SkipIfUnavailable();
        var result = await Users.Create(new CreateUserModel
        {
            Login = "moderator1",
            Name = "Модератор",
            Email = "mod1@example.com",
            Role = "Moderator",
            Password = "TempPassword123",
        }, CancellationToken.None);

        result.Login.Should().Be("moderator1");
        result.Role.Should().Be("Moderator");
        result.PasswordIsTemporary.Should().BeTrue();
        result.Blocked.Should().BeFalse();
        var stored = await Context.Set<User>().AsNoTracking().SingleAsync(x => x.Id == result.Id);
        stored.PasswordHash.Should().NotBe("TempPassword123");
        stored.LoginLowerCase.Should().Be("moderator1");
        stored.EmailLowerCase.Should().Be("mod1@example.com");
        SecurityHelper.VerifyPassword("TempPassword123", stored.PasswordSalt, stored.PasswordHash).Should().BeTrue();
        // Временный пароль обязывает сменить его при первом входе
        var login = await Auth.Login(new LoginModel { Login = "moderator1", Password = "TempPassword123" }, CancellationToken.None);
        login.User.PasswordIsTemporary.Should().BeTrue();
    }

    /// <summary>
    /// Повторный логин (в любом регистре) — конфликт
    /// </summary>
    [SkippableFact]
    public async Task CreateWithDuplicateLoginShouldThrowDuplicate()
    {
        SkipIfUnavailable();
        await SeedUser("reader");
        var act = () => Users.Create(new CreateUserModel { Login = "READER", Name = "Дубль", Role = "User", Password = "TempPassword123" }, CancellationToken.None);
        await act.Should().ThrowAsync<DumaLensDuplicateException>();
    }

    /// <summary>
    /// Повторная электронная почта — конфликт
    /// </summary>
    [SkippableFact]
    public async Task CreateWithDuplicateEmailShouldThrowDuplicate()
    {
        SkipIfUnavailable();
        await Users.Create(new CreateUserModel { Login = "first", Name = "Первый", Email = "same@example.com", Role = "User", Password = "TempPassword123" }, CancellationToken.None);
        var act = () => Users.Create(new CreateUserModel { Login = "second", Name = "Второй", Email = "SAME@example.com", Role = "User", Password = "TempPassword123" }, CancellationToken.None);
        await act.Should().ThrowAsync<DumaLensDuplicateException>();
    }

    /// <summary>
    /// Несколько пользователей без электронной почты не конфликтуют (пустые значения вне уникального индекса)
    /// </summary>
    [SkippableFact]
    public async Task CreateWithoutEmailShouldNotCollide()
    {
        SkipIfUnavailable();
        await Users.Create(new CreateUserModel { Login = "noemail1", Name = "Первый", Role = "User", Password = "TempPassword123" }, CancellationToken.None);
        var second = await Users.Create(new CreateUserModel { Login = "noemail2", Name = "Второй", Role = "User", Password = "TempPassword123" }, CancellationToken.None);
        second.Email.Should().BeEmpty();
    }

    /// <summary>
    /// Неизвестная роль — ошибка валидации
    /// </summary>
    [SkippableFact]
    public async Task CreateWithUnknownRoleShouldThrowValidation()
    {
        SkipIfUnavailable();
        var act = () => Users.Create(new CreateUserModel { Login = "x", Name = "Икс", Role = "Superuser", Password = "TempPassword123" }, CancellationToken.None);
        await act.Should().ThrowAsync<DumaLensValidationException>();
    }

    /// <summary>
    /// Пагинация списка: страницы не пересекаются, общее число корректно
    /// </summary>
    [SkippableFact]
    public async Task GetPageShouldPaginate()
    {
        SkipIfUnavailable();
        for (var i = 0; i < 5; i++)
        {
            await SeedUser($"user{i:D2}");
        }

        var first = await Users.GetPage(new UserListQueryModel { Page = 1, PageSize = 2 }, CancellationToken.None);
        var second = await Users.GetPage(new UserListQueryModel { Page = 2, PageSize = 2 }, CancellationToken.None);
        var third = await Users.GetPage(new UserListQueryModel { Page = 3, PageSize = 2 }, CancellationToken.None);

        first.TotalCount.Should().Be(5);
        first.Items.Select(x => x.Login).Should().ContainInOrder("user00", "user01");
        second.Items.Select(x => x.Login).Should().ContainInOrder("user02", "user03");
        third.Items.Select(x => x.Login).Should().ContainInOrder("user04");
    }

    /// <summary>
    /// Смена роли меняет метку безопасности и отзывает сессии
    /// </summary>
    [SkippableFact]
    public async Task UpdateRoleShouldRotateStampAndRevokeSessions()
    {
        SkipIfUnavailable();
        var user = await SeedUser("reader");
        var login = await Auth.Login(new LoginModel { Login = "reader", Password = DefaultPassword }, CancellationToken.None);

        var updated = await Users.Update(user.Id, new UpdateUserModel { Name = "reader", Role = "Moderator", Blocked = false }, CancellationToken.None);

        updated.Role.Should().Be("Moderator");
        var stored = await UserRead.GetById(user.Id, CancellationToken.None);
        stored!.SecurityStamp.Should().NotBe(user.SecurityStamp);
        var refresh = () => Auth.Refresh(new RefreshRequestModel { RefreshToken = login.RefreshToken }, CancellationToken.None);
        await refresh.Should().ThrowAsync<DumaLensUnauthorizedException>();
    }

    /// <summary>
    /// Блокировка завершает сессии и запрещает вход
    /// </summary>
    [SkippableFact]
    public async Task UpdateBlockShouldRevokeSessionsAndPreventLogin()
    {
        SkipIfUnavailable();
        var user = await SeedUser("reader");
        var login = await Auth.Login(new LoginModel { Login = "reader", Password = DefaultPassword }, CancellationToken.None);

        await Users.Update(user.Id, new UpdateUserModel { Name = "reader", Role = "User", Blocked = true }, CancellationToken.None);

        var refresh = () => Auth.Refresh(new RefreshRequestModel { RefreshToken = login.RefreshToken }, CancellationToken.None);
        await refresh.Should().ThrowAsync<DumaLensUnauthorizedException>();
        var loginAct = () => Auth.Login(new LoginModel { Login = "reader", Password = DefaultPassword }, CancellationToken.None);
        await loginAct.Should().ThrowAsync<DumaLensUnauthorizedException>();
    }

    /// <summary>
    /// Обновление профиля без смены роли и блокировки не трогает сессии
    /// </summary>
    [SkippableFact]
    public async Task UpdateProfileOnlyShouldKeepSessions()
    {
        SkipIfUnavailable();
        var user = await SeedUser("reader");
        var login = await Auth.Login(new LoginModel { Login = "reader", Password = DefaultPassword }, CancellationToken.None);

        var updated = await Users.Update(user.Id, new UpdateUserModel { Name = "Новое имя", Email = "reader@example.com", Role = "User", Blocked = false, Note = "заметка" }, CancellationToken.None);

        updated.Name.Should().Be("Новое имя");
        updated.Note.Should().Be("заметка");
        var refreshed = await Auth.Refresh(new RefreshRequestModel { RefreshToken = login.RefreshToken }, CancellationToken.None);
        refreshed.User.Id.Should().Be(user.Id);
    }

    /// <summary>
    /// Последнего активного администратора нельзя разжаловать или заблокировать
    /// </summary>
    [SkippableFact]
    public async Task UpdateLastAdministratorShouldThrowValidation()
    {
        SkipIfUnavailable();
        var admin = await SeedUser("admin", Role.Administrator);

        var demote = () => Users.Update(admin.Id, new UpdateUserModel { Name = "admin", Role = "User", Blocked = false }, CancellationToken.None);
        await demote.Should().ThrowAsync<DumaLensValidationException>();

        var block = () => Users.Update(admin.Id, new UpdateUserModel { Name = "admin", Role = "Administrator", Blocked = true }, CancellationToken.None);
        await block.Should().ThrowAsync<DumaLensValidationException>();
    }

    /// <summary>
    /// При наличии другого активного администратора разжалование разрешено
    /// </summary>
    [SkippableFact]
    public async Task UpdateAdministratorWhenAnotherExistsShouldSucceed()
    {
        SkipIfUnavailable();
        var first = await SeedUser("admin1", Role.Administrator);
        await SeedUser("admin2", Role.Administrator);

        var updated = await Users.Update(first.Id, new UpdateUserModel { Name = "admin1", Role = "User", Blocked = false }, CancellationToken.None);

        updated.Role.Should().Be("User");
    }

    /// <summary>
    /// Сброс пароля: временный пароль работает, старые сессии завершены, метка изменена
    /// </summary>
    [SkippableFact]
    public async Task ResetPasswordShouldIssueTemporaryAndRevokeSessions()
    {
        SkipIfUnavailable();
        var user = await SeedUser("reader");
        var login = await Auth.Login(new LoginModel { Login = "reader", Password = DefaultPassword }, CancellationToken.None);

        var result = await Users.ResetPassword(user.Id, CancellationToken.None);

        result.TemporaryPassword.Should().NotBeNullOrEmpty();
        result.TemporaryPassword.Length.Should().BeGreaterThanOrEqualTo(8);
        var stored = await UserRead.GetById(user.Id, CancellationToken.None);
        stored!.PasswordIsTemporary.Should().BeTrue();
        stored.SecurityStamp.Should().NotBe(user.SecurityStamp);
        var refresh = () => Auth.Refresh(new RefreshRequestModel { RefreshToken = login.RefreshToken }, CancellationToken.None);
        await refresh.Should().ThrowAsync<DumaLensUnauthorizedException>();
        var oldPassword = () => Auth.Login(new LoginModel { Login = "reader", Password = DefaultPassword }, CancellationToken.None);
        await oldPassword.Should().ThrowAsync<DumaLensUnauthorizedException>();
        var newLogin = await Auth.Login(new LoginModel { Login = "reader", Password = result.TemporaryPassword }, CancellationToken.None);
        newLogin.User.PasswordIsTemporary.Should().BeTrue();
    }

    /// <summary>
    /// Сброс пароля несуществующего пользователя — 404
    /// </summary>
    [SkippableFact]
    public async Task ResetPasswordForUnknownUserShouldThrowNotFound()
    {
        SkipIfUnavailable();
        var act = () => Users.ResetPassword(Guid.NewGuid(), CancellationToken.None);
        await act.Should().ThrowAsync<DumaLensNotFoundException>();
    }

    /// <summary>
    /// Обновление с чужой почтой — конфликт
    /// </summary>
    [SkippableFact]
    public async Task UpdateWithDuplicateEmailShouldThrowDuplicate()
    {
        SkipIfUnavailable();
        var first = await Users.Create(new CreateUserModel { Login = "first", Name = "Первый", Email = "same@example.com", Role = "User", Password = "TempPassword123" }, CancellationToken.None);
        var second = await Users.Create(new CreateUserModel { Login = "second", Name = "Второй", Role = "User", Password = "TempPassword123" }, CancellationToken.None);

        var act = () => Users.Update(second.Id, new UpdateUserModel { Name = "Второй", Email = "SAME@example.com", Role = "User", Blocked = false }, CancellationToken.None);
        await act.Should().ThrowAsync<DumaLensDuplicateException>();
    }

    /// <summary>
    /// Обновление с собственной почтой разрешено (исключение себя из проверки уникальности)
    /// </summary>
    [SkippableFact]
    public async Task UpdateWithOwnEmailShouldSucceed()
    {
        SkipIfUnavailable();
        var user = await Users.Create(new CreateUserModel { Login = "owner", Name = "Владелец", Email = "own@example.com", Role = "User", Password = "TempPassword123" }, CancellationToken.None);

        var updated = await Users.Update(user.Id, new UpdateUserModel { Name = "Владелец", Email = "OWN@example.com", Role = "User", Blocked = false }, CancellationToken.None);

        updated.Email.Should().Be("OWN@example.com");
    }

    /// <summary>
    /// Пропуск обязательного признака блокировки — ошибка валидации (молчаливая разблокировка недопустима)
    /// </summary>
    [SkippableFact]
    public async Task UpdateWithoutBlockedShouldThrowValidation()
    {
        SkipIfUnavailable();
        var user = await SeedUser("reader");
        var act = () => Users.Update(user.Id, new UpdateUserModel { Name = "reader", Role = "User", Blocked = null }, CancellationToken.None);
        await act.Should().ThrowAsync<DumaLensValidationException>();
    }

    /// <summary>
    /// Номер страницы сверх верхней границы — ошибка валидации (защита Skip от переполнения)
    /// </summary>
    [SkippableFact]
    public async Task GetPageWithOverflowPageShouldThrowValidation()
    {
        SkipIfUnavailable();
        var act = () => Users.GetPage(new UserListQueryModel { Page = 100_001, PageSize = 100 }, CancellationToken.None);
        await act.Should().ThrowAsync<DumaLensValidationException>();
    }
}