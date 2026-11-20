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
/// Интеграционные тесты аутентификации (Э9, 07-security.md §5) на одноразовой PostgreSQL-базе:
/// реальные репозитории без моков, ротация refresh, отзыв при повторном использовании,
/// метка безопасности, счётчик попыток и блокировка, смена пароля
/// </summary>
public sealed class AuthServiceTests : AuthTestBase
{
    /// <summary>
    /// Вход: пара токенов и профиль; в базе хранится хэш refresh-токена, а не сам токен
    /// </summary>
    [SkippableFact]
    public async Task LoginShouldIssueTokenPairAndStoreHashOnly()
    {
        SkipIfUnavailable();
        var user = await SeedUser("admin", Role.Administrator);

        var result = await Auth.Login(new LoginModel { Login = "admin", Password = DefaultPassword }, CancellationToken.None);

        result.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
        result.User.Id.Should().Be(user.Id);
        result.User.Login.Should().Be("admin");
        result.User.Role.Should().Be("Administrator");
        result.User.PasswordIsTemporary.Should().BeFalse();
        var stored = await Context.Set<RefreshToken>().AsNoTracking().SingleAsync(x => x.UserId == user.Id);
        stored.TokenHash.Should().NotBe(result.RefreshToken);
        stored.SecurityStamp.Should().Be(user.SecurityStamp);
        stored.RevokedAt.Should().BeNull();
        stored.ExpiresAt.Should().Be(StartTime.AddDays(Options.RefreshTokenLifetimeDays));
    }

    /// <summary>
    /// Вход регистронезависим по логину
    /// </summary>
    [SkippableFact]
    public async Task LoginShouldBeCaseInsensitive()
    {
        SkipIfUnavailable();
        await SeedUser("Admin");
        var result = await Auth.Login(new LoginModel { Login = "ADMIN", Password = DefaultPassword }, CancellationToken.None);
        result.User.Login.Should().Be("Admin");
    }

    /// <summary>
    /// Неверный пароль: 401 и счётчик неудачных попыток растёт
    /// </summary>
    [SkippableFact]
    public async Task LoginWithWrongPasswordShouldThrowAndIncrementCounter()
    {
        SkipIfUnavailable();
        var user = await SeedUser("reader");

        var act = () => Auth.Login(new LoginModel { Login = "reader", Password = "WrongPassword1" }, CancellationToken.None);

        await act.Should().ThrowAsync<DumaLensUnauthorizedException>();
        var stored = await UserRead.GetById(user.Id, CancellationToken.None);
        stored!.LoginAttemptFailedCount.Should().Be(1);
        stored.LastFailedLoginAt.Should().Be(StartTime);
    }

    /// <summary>
    /// Неизвестный логин: 401 без раскрытия существования пользователя
    /// </summary>
    [SkippableFact]
    public async Task LoginWithUnknownUserShouldThrowUnauthorized()
    {
        SkipIfUnavailable();
        var act = () => Auth.Login(new LoginModel { Login = "ghost", Password = DefaultPassword }, CancellationToken.None);
        await act.Should().ThrowAsync<DumaLensUnauthorizedException>();
    }

    /// <summary>
    /// Заблокированный пользователь не может войти
    /// </summary>
    [SkippableFact]
    public async Task LoginBlockedUserShouldThrowUnauthorized()
    {
        SkipIfUnavailable();
        await SeedUser("blocked", blocked: true);
        var act = () => Auth.Login(new LoginModel { Login = "blocked", Password = DefaultPassword }, CancellationToken.None);
        await act.Should().ThrowAsync<DumaLensUnauthorizedException>();
    }

    /// <summary>
    /// После N неудачных попыток вход блокируется на время T даже с верным паролем; после окна — снова работает
    /// </summary>
    [SkippableFact]
    public async Task LoginShouldLockOutAfterMaxAttemptsAndRecoverAfterWindow()
    {
        SkipIfUnavailable();
        var user = await SeedUser("reader");
        for (var i = 0; i < Options.MaxFailedLoginAttempts; i++)
        {
            var wrong = () => Auth.Login(new LoginModel { Login = "reader", Password = "WrongPassword1" }, CancellationToken.None);
            await wrong.Should().ThrowAsync<DumaLensUnauthorizedException>();
        }

        var locked = () => Auth.Login(new LoginModel { Login = "reader", Password = DefaultPassword }, CancellationToken.None);
        await locked.Should().ThrowAsync<DumaLensUnauthorizedException>("действует временная блокировка");

        Clock.Time = StartTime.AddMinutes(Options.LoginLockoutMinutes + 1);
        var result = await Auth.Login(new LoginModel { Login = "reader", Password = DefaultPassword }, CancellationToken.None);
        result.User.Id.Should().Be(user.Id);
        var stored = await UserRead.GetById(user.Id, CancellationToken.None);
        stored!.LoginAttemptFailedCount.Should().Be(0);
        stored.LastFailedLoginAt.Should().BeNull();
    }

    /// <summary>
    /// Временный пароль отражается в профиле (принудительная смена при первом входе)
    /// </summary>
    [SkippableFact]
    public async Task LoginWithTemporaryPasswordShouldFlagProfile()
    {
        SkipIfUnavailable();
        await SeedUser("fresh", temporary: true);
        var result = await Auth.Login(new LoginModel { Login = "fresh", Password = DefaultPassword }, CancellationToken.None);
        result.User.PasswordIsTemporary.Should().BeTrue();
    }

    /// <summary>
    /// Повторный вход отзывает предыдущий refresh-токен (один активный на пользователя)
    /// </summary>
    [SkippableFact]
    public async Task LoginShouldRevokePreviousRefreshToken()
    {
        SkipIfUnavailable();
        var user = await SeedUser("reader");
        var first = await Auth.Login(new LoginModel { Login = "reader", Password = DefaultPassword }, CancellationToken.None);

        Clock.Time = StartTime.AddMinutes(1);
        var second = await Auth.Login(new LoginModel { Login = "reader", Password = DefaultPassword }, CancellationToken.None);

        var tokens = await Context.Set<RefreshToken>().AsNoTracking().Where(x => x.UserId == user.Id).ToListAsync();
        tokens.Should().HaveCount(2);
        tokens.Count(x => x.RevokedAt == null).Should().Be(1);
        second.RefreshToken.Should().NotBe(first.RefreshToken);
        var oldHash = SecurityHelper.HashRefreshToken(first.RefreshToken);
        tokens.Single(x => x.TokenHash == oldHash).RevokedAt.Should().Be(StartTime.AddMinutes(1));
    }

    /// <summary>
    /// Обновление: ротация пары, старый токен отозван, новый активен
    /// </summary>
    [SkippableFact]
    public async Task RefreshShouldRotateTokenPair()
    {
        SkipIfUnavailable();
        var user = await SeedUser("reader");
        var login = await Auth.Login(new LoginModel { Login = "reader", Password = DefaultPassword }, CancellationToken.None);

        Clock.Time = StartTime.AddMinutes(5);
        var refreshed = await Auth.Refresh(new RefreshRequestModel { RefreshToken = login.RefreshToken }, CancellationToken.None);

        refreshed.RefreshToken.Should().NotBe(login.RefreshToken);
        refreshed.AccessToken.Should().NotBeNullOrEmpty();
        refreshed.User.Id.Should().Be(user.Id);
        var tokens = await Context.Set<RefreshToken>().AsNoTracking().Where(x => x.UserId == user.Id).ToListAsync();
        tokens.Count(x => x.RevokedAt == null).Should().Be(1);
        var oldHash = SecurityHelper.HashRefreshToken(login.RefreshToken);
        tokens.Single(x => x.TokenHash == oldHash).RevokedAt.Should().Be(StartTime.AddMinutes(5));
    }

    /// <summary>
    /// Просроченный refresh-токен не обновляет пару
    /// </summary>
    [SkippableFact]
    public async Task RefreshWithExpiredTokenShouldThrowUnauthorized()
    {
        SkipIfUnavailable();
        await SeedUser("reader");
        var login = await Auth.Login(new LoginModel { Login = "reader", Password = DefaultPassword }, CancellationToken.None);

        Clock.Time = StartTime.AddDays(Options.RefreshTokenLifetimeDays + 1);
        var act = () => Auth.Refresh(new RefreshRequestModel { RefreshToken = login.RefreshToken }, CancellationToken.None);
        await act.Should().ThrowAsync<DumaLensUnauthorizedException>();
    }

    /// <summary>
    /// Повторное использование отозванного refresh-токена отзывает все сессии и меняет метку безопасности
    /// </summary>
    [SkippableFact]
    public async Task RefreshReuseOfRevokedTokenShouldRevokeAllSessions()
    {
        SkipIfUnavailable();
        var user = await SeedUser("reader");
        var login = await Auth.Login(new LoginModel { Login = "reader", Password = DefaultPassword }, CancellationToken.None);
        var refreshed = await Auth.Refresh(new RefreshRequestModel { RefreshToken = login.RefreshToken }, CancellationToken.None);

        // Украденная копия старого токена используется повторно
        var act = () => Auth.Refresh(new RefreshRequestModel { RefreshToken = login.RefreshToken }, CancellationToken.None);
        await act.Should().ThrowAsync<DumaLensUnauthorizedException>();

        // Все сессии завершены: и новый токен больше не работает
        var followUp = () => Auth.Refresh(new RefreshRequestModel { RefreshToken = refreshed.RefreshToken }, CancellationToken.None);
        await followUp.Should().ThrowAsync<DumaLensUnauthorizedException>();
        var active = await Context.Set<RefreshToken>().AsNoTracking().CountAsync(x => x.UserId == user.Id && x.RevokedAt == null);
        active.Should().Be(0);
        var stored = await UserRead.GetById(user.Id, CancellationToken.None);
        stored!.SecurityStamp.Should().NotBe(user.SecurityStamp);
    }

    /// <summary>
    /// Изменение метки безопасности пользователя инвалидирует refresh-токен
    /// </summary>
    [SkippableFact]
    public async Task RefreshAfterSecurityStampChangeShouldThrowUnauthorized()
    {
        SkipIfUnavailable();
        var user = await SeedUser("reader");
        var login = await Auth.Login(new LoginModel { Login = "reader", Password = DefaultPassword }, CancellationToken.None);
        var entity = await Context.Set<User>().SingleAsync(x => x.Id == user.Id);
        entity.SecurityStamp = Guid.NewGuid();
        await Context.SaveChangesAsync();

        var act = () => Auth.Refresh(new RefreshRequestModel { RefreshToken = login.RefreshToken }, CancellationToken.None);
        await act.Should().ThrowAsync<DumaLensUnauthorizedException>();
    }

    /// <summary>
    /// Заблокированный пользователь не может обновить токены
    /// </summary>
    [SkippableFact]
    public async Task RefreshBlockedUserShouldThrowUnauthorized()
    {
        SkipIfUnavailable();
        var user = await SeedUser("reader");
        var login = await Auth.Login(new LoginModel { Login = "reader", Password = DefaultPassword }, CancellationToken.None);
        var entity = await Context.Set<User>().SingleAsync(x => x.Id == user.Id);
        entity.Blocked = true;
        await Context.SaveChangesAsync();

        var act = () => Auth.Refresh(new RefreshRequestModel { RefreshToken = login.RefreshToken }, CancellationToken.None);
        await act.Should().ThrowAsync<DumaLensUnauthorizedException>();
    }

    /// <summary>
    /// Временный пароль не блокирует обновление: смену принуждает авторизация Api,
    /// иначе смена пароля после истечения access-токена была бы невозможна
    /// </summary>
    [SkippableFact]
    public async Task RefreshWithTemporaryPasswordShouldSucceedAndKeepFlag()
    {
        SkipIfUnavailable();
        var user = await SeedUser("fresh", temporary: true);
        var login = await Auth.Login(new LoginModel { Login = "fresh", Password = DefaultPassword }, CancellationToken.None);

        var refreshed = await Auth.Refresh(new RefreshRequestModel { RefreshToken = login.RefreshToken }, CancellationToken.None);

        refreshed.User.PasswordIsTemporary.Should().BeTrue();
        refreshed.RefreshToken.Should().NotBe(login.RefreshToken);
        var stored = await UserRead.GetById(user.Id, CancellationToken.None);
        stored!.PasswordIsTemporary.Should().BeTrue();
    }

    /// <summary>
    /// Повторное использование только что отозванного токена (grace-окно) — гонка параллельных
    /// обновлений: выпускается новая пара, сессии не отзываются
    /// </summary>
    [SkippableFact]
    public async Task RefreshReuseWithinGraceWindowShouldIssueNewPair()
    {
        SkipIfUnavailable();
        Options.RefreshReuseGraceSeconds = 30;
        await SeedUser("reader");
        var login = await Auth.Login(new LoginModel { Login = "reader", Password = DefaultPassword }, CancellationToken.None);
        var first = await Auth.Refresh(new RefreshRequestModel { RefreshToken = login.RefreshToken }, CancellationToken.None);

        // Вторая вкладка предъявляет только что отозванный токен в пределах grace-окна
        var second = await Auth.Refresh(new RefreshRequestModel { RefreshToken = login.RefreshToken }, CancellationToken.None);

        second.RefreshToken.Should().NotBe(first.RefreshToken);
        var active = await Context.Set<RefreshToken>().AsNoTracking().CountAsync(x => x.RevokedAt == null);
        active.Should().Be(1);
        // Последняя пара рабочая: сессия жива
        var third = await Auth.Refresh(new RefreshRequestModel { RefreshToken = second.RefreshToken }, CancellationToken.None);
        third.AccessToken.Should().NotBeNullOrEmpty();
    }

    /// <summary>
    /// Повторное использование отозванного токена вне grace-окна — кража: все сессии отзываются
    /// </summary>
    [SkippableFact]
    public async Task RefreshReuseOutsideGraceWindowShouldRevokeAllSessions()
    {
        SkipIfUnavailable();
        Options.RefreshReuseGraceSeconds = 30;
        var user = await SeedUser("reader");
        var login = await Auth.Login(new LoginModel { Login = "reader", Password = DefaultPassword }, CancellationToken.None);
        var refreshed = await Auth.Refresh(new RefreshRequestModel { RefreshToken = login.RefreshToken }, CancellationToken.None);

        Clock.Time = StartTime.AddSeconds(Options.RefreshReuseGraceSeconds + 1);
        var act = () => Auth.Refresh(new RefreshRequestModel { RefreshToken = login.RefreshToken }, CancellationToken.None);
        await act.Should().ThrowAsync<DumaLensUnauthorizedException>();

        var followUp = () => Auth.Refresh(new RefreshRequestModel { RefreshToken = refreshed.RefreshToken }, CancellationToken.None);
        await followUp.Should().ThrowAsync<DumaLensUnauthorizedException>();
        var stored = await UserRead.GetById(user.Id, CancellationToken.None);
        stored!.SecurityStamp.Should().NotBe(user.SecurityStamp);
    }

    /// <summary>
    /// Неудачная смена пароля учитывается в счётчике блокировки (защита от брутфорса текущего пароля)
    /// </summary>
    [SkippableFact]
    public async Task ChangePasswordWithWrongCurrentShouldIncrementCounter()
    {
        SkipIfUnavailable();
        var user = await SeedUser("reader");
        var act = () => Auth.ChangePassword(user.Id, new ChangePasswordModel { CurrentPassword = "WrongPassword1", NewPassword = "NewPassword456" }, CancellationToken.None);
        await act.Should().ThrowAsync<DumaLensUnauthorizedException>();
        var stored = await UserRead.GetById(user.Id, CancellationToken.None);
        stored!.LoginAttemptFailedCount.Should().Be(1);
        stored.LastFailedLoginAt.Should().Be(StartTime);
    }

    /// <summary>
    /// После серии неудач смена пароля блокируется вместе с входом
    /// </summary>
    [SkippableFact]
    public async Task ChangePasswordShouldBeDeniedWhenLockedOut()
    {
        SkipIfUnavailable();
        var user = await SeedUser("reader");
        for (var i = 0; i < Options.MaxFailedLoginAttempts; i++)
        {
            var wrong = () => Auth.ChangePassword(user.Id, new ChangePasswordModel { CurrentPassword = "WrongPassword1", NewPassword = "NewPassword456" }, CancellationToken.None);
            await wrong.Should().ThrowAsync<DumaLensUnauthorizedException>();
        }
        var locked = () => Auth.ChangePassword(user.Id, new ChangePasswordModel { CurrentPassword = DefaultPassword, NewPassword = "NewPassword456" }, CancellationToken.None);
        await locked.Should().ThrowAsync<DumaLensUnauthorizedException>();
    }

    /// <summary>
    /// Заблокированный пользователь не может сменить пароль даже с валидным access-токеном
    /// </summary>
    [SkippableFact]
    public async Task ChangePasswordShouldBeDeniedForBlockedUser()
    {
        SkipIfUnavailable();
        var user = await SeedUser("reader", blocked: true);
        var act = () => Auth.ChangePassword(user.Id, new ChangePasswordModel { CurrentPassword = DefaultPassword, NewPassword = "NewPassword456" }, CancellationToken.None);
        await act.Should().ThrowAsync<DumaLensUnauthorizedException>();
    }

    /// <summary>
    /// Все отказы входа возвращают единое сообщение: существование и состояние учётной записи не раскрываются
    /// </summary>
    [SkippableFact]
    public async Task LoginFailuresShouldUseUnifiedMessage()
    {
        SkipIfUnavailable();
        await SeedUser("reader");
        await SeedUser("blocked", blocked: true);
        const string expected = "Неверный логин или пароль";

        var unknown = () => Auth.Login(new LoginModel { Login = "ghost", Password = DefaultPassword }, CancellationToken.None);
        (await unknown.Should().ThrowAsync<DumaLensUnauthorizedException>()).Which.Message.Should().Be(expected);
        var wrong = () => Auth.Login(new LoginModel { Login = "reader", Password = "WrongPassword1" }, CancellationToken.None);
        (await wrong.Should().ThrowAsync<DumaLensUnauthorizedException>()).Which.Message.Should().Be(expected);
        var blocked = () => Auth.Login(new LoginModel { Login = "blocked", Password = DefaultPassword }, CancellationToken.None);
        (await blocked.Should().ThrowAsync<DumaLensUnauthorizedException>()).Which.Message.Should().Be(expected);

        for (var i = 0; i < Options.MaxFailedLoginAttempts; i++)
        {
            var fail = () => Auth.Login(new LoginModel { Login = "reader", Password = "WrongPassword1" }, CancellationToken.None);
            await fail.Should().ThrowAsync<DumaLensUnauthorizedException>();
        }
        var locked = () => Auth.Login(new LoginModel { Login = "reader", Password = DefaultPassword }, CancellationToken.None);
        (await locked.Should().ThrowAsync<DumaLensUnauthorizedException>()).Which.Message.Should().Be(expected);
    }

    /// <summary>
    /// Неизвестный refresh-токен не обновляет пару
    /// </summary>
    [SkippableFact]
    public async Task RefreshWithUnknownTokenShouldThrowUnauthorized()
    {
        SkipIfUnavailable();
        var act = () => Auth.Refresh(new RefreshRequestModel { RefreshToken = SecurityHelper.GenerateRefreshToken() }, CancellationToken.None);
        await act.Should().ThrowAsync<DumaLensUnauthorizedException>();
    }

    /// <summary>
    /// Выход отзывает активный refresh-токен
    /// </summary>
    [SkippableFact]
    public async Task LogoutShouldRevokeActiveRefreshToken()
    {
        SkipIfUnavailable();
        var user = await SeedUser("reader");
        var login = await Auth.Login(new LoginModel { Login = "reader", Password = DefaultPassword }, CancellationToken.None);

        await Auth.Logout(user.Id, CancellationToken.None);

        var act = () => Auth.Refresh(new RefreshRequestModel { RefreshToken = login.RefreshToken }, CancellationToken.None);
        await act.Should().ThrowAsync<DumaLensUnauthorizedException>();
    }

    /// <summary>
    /// Смена пароля: новый пароль работает, старый — нет; метка меняется, все сессии завершаются
    /// </summary>
    [SkippableFact]
    public async Task ChangePasswordShouldRotateStampAndRevokeSessions()
    {
        SkipIfUnavailable();
        var user = await SeedUser("reader", temporary: true);
        var login = await Auth.Login(new LoginModel { Login = "reader", Password = DefaultPassword }, CancellationToken.None);

        await Auth.ChangePassword(user.Id, new ChangePasswordModel { CurrentPassword = DefaultPassword, NewPassword = "NewPassword456" }, CancellationToken.None);

        var stored = await UserRead.GetById(user.Id, CancellationToken.None);
        stored!.SecurityStamp.Should().NotBe(user.SecurityStamp);
        stored.PasswordIsTemporary.Should().BeFalse();
        var oldRefresh = () => Auth.Refresh(new RefreshRequestModel { RefreshToken = login.RefreshToken }, CancellationToken.None);
        await oldRefresh.Should().ThrowAsync<DumaLensUnauthorizedException>();
        var oldPassword = () => Auth.Login(new LoginModel { Login = "reader", Password = DefaultPassword }, CancellationToken.None);
        await oldPassword.Should().ThrowAsync<DumaLensUnauthorizedException>();
        var result = await Auth.Login(new LoginModel { Login = "reader", Password = "NewPassword456" }, CancellationToken.None);
        result.User.PasswordIsTemporary.Should().BeFalse();
    }

    /// <summary>
    /// Смена пароля с неверным текущим паролем отклоняется
    /// </summary>
    [SkippableFact]
    public async Task ChangePasswordWithWrongCurrentShouldThrowUnauthorized()
    {
        SkipIfUnavailable();
        var user = await SeedUser("reader");
        var act = () => Auth.ChangePassword(user.Id, new ChangePasswordModel { CurrentPassword = "WrongPassword1", NewPassword = "NewPassword456" }, CancellationToken.None);
        await act.Should().ThrowAsync<DumaLensUnauthorizedException>();
    }

    /// <summary>
    /// Слишком короткий новый пароль — ошибка валидации
    /// </summary>
    [SkippableFact]
    public async Task ChangePasswordWithShortNewShouldThrowValidation()
    {
        SkipIfUnavailable();
        var user = await SeedUser("reader");
        var act = () => Auth.ChangePassword(user.Id, new ChangePasswordModel { CurrentPassword = DefaultPassword, NewPassword = "short" }, CancellationToken.None);
        await act.Should().ThrowAsync<DumaLensValidationException>();
    }

    /// <summary>
    /// Профиль пользователя возвращается по идентификатору; отсутствующий — 404
    /// </summary>
    [SkippableFact]
    public async Task GetProfileShouldReturnProfileOrThrowNotFound()
    {
        SkipIfUnavailable();
        var user = await SeedUser("reader", Role.Moderator);
        var profile = await Auth.GetProfile(user.Id, CancellationToken.None);
        profile.Login.Should().Be("reader");
        profile.Role.Should().Be("Moderator");

        var act = () => Auth.GetProfile(Guid.NewGuid(), CancellationToken.None);
        await act.Should().ThrowAsync<DumaLensNotFoundException>();
    }

    /// <summary>
    /// Пустой логин — ошибка валидации до обращения к базе
    /// </summary>
    [SkippableFact]
    public async Task LoginWithEmptyLoginShouldThrowValidation()
    {
        SkipIfUnavailable();
        var act = () => Auth.Login(new LoginModel { Login = "", Password = DefaultPassword }, CancellationToken.None);
        await act.Should().ThrowAsync<DumaLensValidationException>();
    }
}