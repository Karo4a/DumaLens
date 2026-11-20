using DumaLens.Entities;
using DumaLens.Entities.Enums;
using DumaLens.Services.Contracts.Exceptions;
using DumaLens.Services.Contracts.Models;
using DumaLens.Services.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DumaLens.Services.Tests;

/// <summary>
/// Конкурентные сценарии аутентификации и управления пользователями на одноразовой PostgreSQL-базе:
/// параллельные login/refresh одного пользователя (уникальный индекс активной сессии),
/// grace-окно повторного использования, защита последнего администратора при параллельном разжаловании,
/// параллельное создание дубликата. Каждый участник работает через независимый DbContext
/// </summary>
public sealed class AuthConcurrencyTests : AuthTestBase
{
    /// <summary>
    /// Параллельный refresh с одним токеном (две вкладки): оба запроса успешны,
    /// активная сессия одна, отзыв всех сессий не срабатывает
    /// </summary>
    [SkippableFact]
    public async Task ConcurrentRefreshWithSameTokenShouldBothSucceed()
    {
        SkipIfUnavailable();
        Options.RefreshReuseGraceSeconds = 30;
        var user = await SeedUser("reader");
        var login = await Auth.Login(new LoginModel { Login = "reader", Password = DefaultPassword }, CancellationToken.None);

        var auth1 = CreateAuthOnNewContext(out var context1);
        var auth2 = CreateAuthOnNewContext(out var context2);
        await using (context1)
        await using (context2)
        {
            var refresh1 = auth1.Refresh(new RefreshRequestModel { RefreshToken = login.RefreshToken }, CancellationToken.None);
            var refresh2 = auth2.Refresh(new RefreshRequestModel { RefreshToken = login.RefreshToken }, CancellationToken.None);
            var results = await Task.WhenAll(refresh1, refresh2);

            results.Should().OnlyContain(r => !string.IsNullOrEmpty(r.AccessToken));
            results[0].RefreshToken.Should().NotBe(results[1].RefreshToken);
        }
        var active = await Context.Set<RefreshToken>().AsNoTracking().CountAsync(x => x.RevokedAt == null);
        active.Should().Be(1);
        var stored = await UserRead.GetById(user.Id, CancellationToken.None);
        stored!.SecurityStamp.Should().Be(user.SecurityStamp, "метка не должна меняться при гонке обновлений");
    }

    /// <summary>
    /// Параллельный вход одного пользователя: оба успешны, активная сессия одна (без 500 от уникального индекса)
    /// </summary>
    [SkippableFact]
    public async Task ConcurrentLoginShouldBothSucceed()
    {
        SkipIfUnavailable();
        await SeedUser("reader");

        var auth1 = CreateAuthOnNewContext(out var context1);
        var auth2 = CreateAuthOnNewContext(out var context2);
        await using (context1)
        await using (context2)
        {
            var login1 = auth1.Login(new LoginModel { Login = "reader", Password = DefaultPassword }, CancellationToken.None);
            var login2 = auth2.Login(new LoginModel { Login = "reader", Password = DefaultPassword }, CancellationToken.None);
            var results = await Task.WhenAll(login1, login2);

            results.Should().OnlyContain(r => !string.IsNullOrEmpty(r.RefreshToken));
        }
        var active = await Context.Set<RefreshToken>().AsNoTracking().CountAsync(x => x.RevokedAt == null);
        active.Should().Be(1);
    }

    /// <summary>
    /// Параллельное разжалование двух последних администраторов: один запрос проходит,
    /// второй отклоняется — система не остаётся без администратора
    /// </summary>
    [SkippableFact]
    public async Task ConcurrentLastAdminDemotionShouldKeepOneAdmin()
    {
        SkipIfUnavailable();
        var admin1 = await SeedUser("admin1", Role.Administrator);
        var admin2 = await SeedUser("admin2", Role.Administrator);

        var users1 = CreateUsersOnNewContext(out var context1);
        var users2 = CreateUsersOnNewContext(out var context2);
        await using (context1)
        await using (context2)
        {
            var demote1 = users1.Update(admin1.Id, new UpdateUserModel { Name = "admin1", Role = "User", Blocked = false }, CancellationToken.None);
            var demote2 = users2.Update(admin2.Id, new UpdateUserModel { Name = "admin2", Role = "User", Blocked = false }, CancellationToken.None);

            var outcomes = await Task.WhenAll(
                demote1.ContinueWith(t => t.IsFaulted ? t.Exception!.InnerException : null, TaskScheduler.Default),
                demote2.ContinueWith(t => t.IsFaulted ? t.Exception!.InnerException : null, TaskScheduler.Default));

            outcomes.Count(e => e is DumaLensValidationException).Should().Be(1, "ровно один запрос разжалования отклоняется");
            outcomes.Count(e => e == null).Should().Be(1, "ровно один запрос разжалования проходит");
        }
        var remaining = await Context.Set<User>().AsNoTracking()
            .CountAsync(x => x.Role.Role == Role.Administrator && !x.Blocked && x.DeletedAt == null);
        remaining.Should().Be(1);
    }

    /// <summary>
    /// Параллельное создание пользователя с одним логином: один успех, второй — конфликт (409), не 500
    /// </summary>
    [SkippableFact]
    public async Task ConcurrentCreateWithSameLoginShouldYieldOneDuplicate()
    {
        SkipIfUnavailable();
        var users1 = CreateUsersOnNewContext(out var context1);
        var users2 = CreateUsersOnNewContext(out var context2);
        await using (context1)
        await using (context2)
        {
            var model = new CreateUserModel { Login = "double", Name = "Дубль", Role = "User", Password = "TempPassword123" };
            var create1 = users1.Create(model, CancellationToken.None);
            var create2 = users2.Create(model, CancellationToken.None);

            var outcomes = await Task.WhenAll(
                create1.ContinueWith(t => t.IsFaulted ? t.Exception!.InnerException : null, TaskScheduler.Default),
                create2.ContinueWith(t => t.IsFaulted ? t.Exception!.InnerException : null, TaskScheduler.Default));

            outcomes.Count(e => e is DumaLensDuplicateException).Should().Be(1);
            outcomes.Count(e => e == null).Should().Be(1);
        }
        var count = await Context.Set<User>().AsNoTracking().CountAsync(x => x.Login == "double");
        count.Should().Be(1);
    }
}