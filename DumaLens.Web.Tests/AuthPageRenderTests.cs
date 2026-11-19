using FluentAssertions;
using Moq;
using System.Net;
using Xunit;

namespace DumaLens.Web.Tests;

/// <summary>
/// Тесты рендера страниц аутентификации (Э9): форма входа доступна анонимно,
/// защищённые страницы во время пререндера показывают загрузку (без падения и без редиректа)
/// </summary>
public class AuthPageRenderTests
{
    private static async Task<string> GetHtml(WebTestFactory factory, string url)
    {
        var html = await factory.CreateClient().GetStringAsync(url);
        return WebUtility.HtmlDecode(html);
    }

    /// <summary>
    /// Страница входа рендерит форму и ссылку «Войти» в навигации
    /// </summary>
    [Fact]
    public async Task LoginShouldRenderForm()
    {
        using var factory = new WebTestFactory();

        var html = await GetHtml(factory, "/login");

        html.Should().Contain("Вход");
        html.Should().Contain("Логин");
        html.Should().Contain("Пароль");
        html.Should().Contain("Войти");
        html.Should().Contain("type=\"password\"");
    }

    /// <summary>
    /// Защищённые страницы при пререндере (состояние ещё не восстановлено) показывают загрузку
    /// </summary>
    [Theory]
    [InlineData("/profile")]
    [InlineData("/change-password")]
    [InlineData("/admin/users")]
    public async Task ProtectedPagesShouldRenderLoadingDuringPrerender(string url)
    {
        using var factory = new WebTestFactory();

        var response = await factory.CreateClient().GetAsync(url);
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        html.Should().Contain("Загрузка");
    }

    /// <summary>
    /// Публичные страницы остаются доступны без входа
    /// </summary>
    [Fact]
    public async Task PublicPagesShouldStayAnonymous()
    {
        using var factory = new WebTestFactory();
        factory.Stats.Setup(s => s.GetOverviewAsync()).ReturnsAsync(TestData.Overview());
        factory.Convocations.Setup(c => c.GetAllAsync()).ReturnsAsync(TestData.Convocations());

        var html = await GetHtml(factory, "/");

        html.Should().Contain("DumaLens");
        html.Should().Contain("Войти");
    }
}