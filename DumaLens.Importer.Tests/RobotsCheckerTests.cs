using DumaLens.Common;
using DumaLens.Importer.Crawl;
using DumaLens.Importer.Import;
using DumaLens.Importer.Tests.Infrastructure;
using FluentAssertions;
using System.Net;

namespace DumaLens.Importer.Tests;

/// <summary>
/// Юнит-тесты разбора robots.txt: запреты для «*» и «dumalens», пустой Disallow,
/// отсутствующий файл, чужие агенты (Э7)
/// </summary>
public sealed class RobotsCheckerTests
{
    /// <summary>
    /// Disallow /vote/ блокирует карточки, но не листинги
    /// </summary>
    [Fact]
    public async Task Load_DisallowVote_BlocksCardsOnly()
    {
        var checker = await LoadRobots("User-agent: *\nDisallow: /vote/\n");

        checker.IsAllowed("/vote/1").Should().BeFalse();
        checker.IsAllowed("/").Should().BeTrue();
        checker.IsAllowed("/?convocation=AAAAAAA8&page=1").Should().BeTrue();
    }

    /// <summary>
    /// Пустой Disallow разрешает всё
    /// </summary>
    [Fact]
    public async Task Load_EmptyDisallow_AllowsAll()
    {
        var checker = await LoadRobots("User-agent: *\nDisallow:\n");

        checker.IsAllowed("/vote/1").Should().BeTrue();
        checker.IsAllowed("/").Should().BeTrue();
    }

    /// <summary>
    /// Отсутствующий robots.txt (404) — обход разрешён по умолчанию
    /// </summary>
    [Fact]
    public async Task Load_MissingRobots_AllowsAll()
    {
        var handler = new StubHttpHandler(StubHttpHandler.Response(HttpStatusCode.NotFound));
        var checker = new RobotsChecker(new HttpClient(handler), new ImportLog(new DateTimeProvider(), null));

        await checker.LoadAsync("http://localhost", CancellationToken.None);

        checker.IsAllowed("/vote/1").Should().BeTrue();
    }

    /// <summary>
    /// Правила именованного агента DumaLens применяются к кроулеру
    /// </summary>
    [Fact]
    public async Task Load_DumaLensAgent_Applies()
    {
        var checker = await LoadRobots("User-agent: DumaLens\nDisallow: /\n");

        checker.IsAllowed("/").Should().BeFalse();
    }

    /// <summary>
    /// Правила чужого агента игнорируются; комментарии не мешают разбору
    /// </summary>
    [Fact]
    public async Task Load_OtherAgent_Ignored()
    {
        var checker = await LoadRobots("# комментарий\nUser-agent: Googlebot\nDisallow: /\n\nUser-agent: *\nDisallow: # пусто\n");

        checker.IsAllowed("/vote/1").Should().BeTrue();
    }

    private static async Task<RobotsChecker> LoadRobots(string robotsText)
    {
        var handler = new StubHttpHandler(StubHttpHandler.Response(HttpStatusCode.OK, robotsText));
        var checker = new RobotsChecker(new HttpClient(handler), new ImportLog(new DateTimeProvider(), null));
        await checker.LoadAsync("http://localhost", CancellationToken.None);
        return checker;
    }
}