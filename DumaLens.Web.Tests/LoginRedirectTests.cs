using Bunit;
using DumaLens.Api.Client;
using DumaLens.Web.Components.Pages;
using DumaLens.Web.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace DumaLens.Web.Tests;

/// <summary>
/// Редирект аутентифицированного пользователя со страницы входа (Э9):
/// свежая загрузка (состояние восстановлено до первого рендера), позднее восстановление
/// состояния (OnChange после инициализатора), временный пароль, анонимный пользователь
/// </summary>
public class LoginRedirectTests : TestContext
{
    private readonly UserState userState = new();

    /// <summary>
    /// Конструктор: состояние на цепь и моки клиентов Api
    /// </summary>
    public LoginRedirectTests()
    {
        Services.AddSingleton(userState);
        Services.AddSingleton(new Mock<IAuthClient>().Object);
        Services.AddSingleton(new Mock<IClientTokenStore>().Object);
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private NavigationManager Navigation => Services.GetRequiredService<NavigationManager>();

    /// <summary>
    /// Свежая загрузка /login с восстановленной сессией уводит на returnUrl
    /// </summary>
    [Fact]
    public void AuthenticatedFreshLoadShouldRedirectToReturnUrl()
    {
        Navigation.NavigateTo("http://localhost/login?returnUrl=%2Fprofile");
        userState.SetFrom(Guid.NewGuid(), "reader", "Читатель", string.Empty, "User", passwordIsTemporary: false);

        RenderComponent<Login>();

        Navigation.Uri.Should().Be("http://localhost/profile");
    }

    /// <summary>
    /// Состояние восстановлено после первого рендера (гонка инициализатора) — редирект по OnChange
    /// </summary>
    [Fact]
    public void StateRestoredAfterRenderShouldRedirect()
    {
        Navigation.NavigateTo("http://localhost/login");
        RenderComponent<Login>();
        Navigation.Uri.Should().Be("http://localhost/login");

        userState.SetFrom(Guid.NewGuid(), "reader", "Читатель", string.Empty, "User", passwordIsTemporary: false);

        Navigation.Uri.Should().Be("http://localhost/");
    }

    /// <summary>
    /// Временный пароль уводит на смену пароля, а не на returnUrl
    /// </summary>
    [Fact]
    public void TemporaryPasswordShouldRedirectToChangePassword()
    {
        Navigation.NavigateTo("http://localhost/login?returnUrl=%2Fprofile");
        userState.SetFrom(Guid.NewGuid(), "fresh", "Новый", string.Empty, "User", passwordIsTemporary: true);

        RenderComponent<Login>();

        Navigation.Uri.Should().Be("http://localhost/change-password");
    }

    /// <summary>
    /// Анонимный пользователь (инициализация завершена) остаётся на странице входа
    /// </summary>
    [Fact]
    public void AnonymousInitializedShouldStayOnLogin()
    {
        Navigation.NavigateTo("http://localhost/login");
        userState.Clear();

        RenderComponent<Login>();

        Navigation.Uri.Should().Be("http://localhost/login");
    }

    /// <summary>
    /// До завершения инициализации редиректа нет (состояние ещё не восстановлено)
    /// </summary>
    [Fact]
    public void UninitializedShouldStayOnLogin()
    {
        Navigation.NavigateTo("http://localhost/login");

        RenderComponent<Login>();

        Navigation.Uri.Should().Be("http://localhost/login");
    }
}