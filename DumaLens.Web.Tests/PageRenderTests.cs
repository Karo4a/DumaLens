using DumaLens.Api.Client;
using FluentAssertions;
using Moq;
using System.Net;
using Xunit;

namespace DumaLens.Web.Tests;

/// <summary>
/// Интеграционные тесты рендера страниц Web-хоста (данные через моки клиентов Api)
/// </summary>
public class PageRenderTests
{
    private static async Task<string> GetHtml(WebTestFactory factory, string url)
    {
        var html = await factory.CreateClient().GetStringAsync(url);
        return WebUtility.HtmlDecode(html);
    }

    private static void SetupReferences(WebTestFactory factory)
    {
        factory.Factions.Setup(f => f.GetAllAsync()).ReturnsAsync(TestData.Factions());
        factory.Convocations.Setup(c => c.GetAllAsync()).ReturnsAsync(TestData.Convocations());
    }

    private static void SetupSearch(WebTestFactory factory)
    {
        factory.Votes
            .Setup(v => v.SearchWithConditionsAsync(
                It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<DateTimeOffset?>(), It.IsAny<DateTimeOffset?>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<IEnumerable<FactionConditionRequestApiModel>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestData.SearchResultS2());
    }

    [Fact]
    public async Task Home_RendersOverviewAndExamples()
    {
        using var factory = new WebTestFactory();
        factory.Stats.Setup(s => s.GetOverviewAsync()).ReturnsAsync(TestData.Overview());
        factory.Convocations.Setup(c => c.GetAllAsync()).ReturnsAsync(TestData.Convocations());

        var html = await GetHtml(factory, "/");

        html.Should().Contain("DumaLens");
        html.Should().Contain("102");
        html.Should().Contain("Голосований");
        html.Should().Contain("VIII созыв");
        html.Should().Contain("S2");
    }

    [Fact]
    public async Task Search_S2_PassesFactionConditionsToClient()
    {
        using var factory = new WebTestFactory();
        SetupReferences(factory);
        SetupSearch(factory);

        var url = "/search?factionConditions%5B0%5D.faction=NL&factionConditions%5B0%5D.result=accept&factionConditions%5B0%5D.operator=gte&factionConditions%5B0%5D.threshold=70&factionConditions%5B0%5D.basis=members"
            + "&factionConditions%5B1%5D.faction=KPRF&factionConditions%5B1%5D.result=decline&factionConditions%5B1%5D.operator=gte&factionConditions%5B1%5D.threshold=50&factionConditions%5B1%5D.basis=members";
        var html = await GetHtml(factory, url);

        html.Should().Contain("увеличения штрафов");
        html.Should().Contain("223");
        factory.Votes.Verify(v => v.SearchWithConditionsAsync(
            It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<DateTimeOffset?>(), It.IsAny<DateTimeOffset?>(),
            It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<int?>(), It.IsAny<int?>(),
            It.Is<IEnumerable<FactionConditionRequestApiModel>?>(c => c != null && MatchesS2(c)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Search_S1_PassesTextQueryAndConvocation()
    {
        using var factory = new WebTestFactory();
        SetupReferences(factory);
        SetupSearch(factory);

        var url = "/search?q=" + Uri.EscapeDataString("алкоголь") + "&convocation=8";
        var html = await GetHtml(factory, url);

        html.Should().Contain("Найдено");
        factory.Votes.Verify(v => v.SearchWithConditionsAsync(
            "алкоголь", 8, null, null, null, null, null, "dateDesc", 1, 20, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task VoteCard_RendersMatrixAndDeputies()
    {
        using var factory = new WebTestFactory();
        factory.Votes.Setup(v => v.GetDetailsAsync(126282)).ReturnsAsync(TestData.VoteDetails126282());

        var html = await GetHtml(factory, "/votes/126282");

        html.Should().Contain("500091-8");
        html.Should().Contain("Депутаты по фракциям");
        html.Should().Contain("Единая Россия");
        html.Should().Contain("1 за");
        html.Should().Contain("89,2%");
    }

    [Fact]
    public async Task VoteCard_NotFound_RendersError()
    {
        using var factory = new WebTestFactory();
        factory.Votes.Setup(v => v.GetDetailsAsync(999999))
            .ThrowsAsync(new ApiException("not found", 404, string.Empty, new Dictionary<string, IEnumerable<string>>(), null));

        var html = await GetHtml(factory, "/votes/999999");

        html.Should().Contain("не найдено");
    }

    [Fact]
    public async Task BillCard_RendersChronology()
    {
        using var factory = new WebTestFactory();
        factory.Bills.Setup(b => b.GetCardAsync("500091-8")).ReturnsAsync(TestData.Bill500091());

        var html = await GetHtml(factory, "/bills/500091-8");

        html.Should().Contain("Хронология голосований");
        html.Should().Contain("СОЗД");
        html.Should().Contain("третье чтение");
    }

    [Fact]
    public async Task Factions_RendersList()
    {
        using var factory = new WebTestFactory();
        factory.Factions.Setup(f => f.GetAllAsync()).ReturnsAsync(TestData.Factions());

        var html = await GetHtml(factory, "/factions");

        html.Should().Contain("Единая Россия");
        html.Should().Contain("Новые люди");
    }

    [Fact]
    public async Task FactionCard_RendersHistoryAndStats()
    {
        using var factory = new WebTestFactory();
        factory.Factions.Setup(f => f.GetCardAsync("ER")).ReturnsAsync(TestData.Faction("ER", "Единая Россия"));

        var html = await GetHtml(factory, "/factions/ER");

        html.Should().Contain("История названий");
        html.Should().Contain("Статистика по созывам");
        html.Should().Contain("Единая Россия");
    }

    [Fact]
    public async Task Convocations_RendersList()
    {
        using var factory = new WebTestFactory();
        factory.Convocations.Setup(c => c.GetAllAsync()).ReturnsAsync(TestData.Convocations());

        var html = await GetHtml(factory, "/convocations");

        html.Should().Contain("VIII созыв");
        html.Should().Contain("17");
    }

    [Fact]
    public async Task Deputies_RendersListByConvocation()
    {
        using var factory = new WebTestFactory();
        factory.Convocations.Setup(c => c.GetAllAsync()).ReturnsAsync(TestData.Convocations());
        factory.Deputies.Setup(d => d.GetByConvocationAsync(It.IsAny<int?>())).ReturnsAsync(TestData.DeputyList());

        var html = await GetHtml(factory, "/deputies");

        html.Should().Contain("Абакаров Хизри Магомедович");
    }

    [Fact]
    public async Task DeputyCard_RendersCountersAndMembership()
    {
        using var factory = new WebTestFactory();
        factory.Deputies.Setup(d => d.GetCardAsync(99113478)).ReturnsAsync(TestData.Deputy());

        var html = await GetHtml(factory, "/deputies/99113478");

        html.Should().Contain("Абакаров Хизри Магомедович");
        html.Should().Contain("Членство во фракциях");
        html.Should().Contain("Участие");
    }

    [Fact]
    public async Task Search_MalformedConditionIndex_DoesNotCrash()
    {
        using var factory = new WebTestFactory();
        SetupReferences(factory);
        SetupSearch(factory);
        var html = await GetHtml(factory, "/search?factionConditions%5B99999999999999999999%5D.faction=ER");
        html.Should().Contain("Найдено");
        factory.Votes.Verify(v => v.SearchWithConditionsAsync(
            It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<DateTimeOffset?>(), It.IsAny<DateTimeOffset?>(),
            It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<int?>(), It.IsAny<int?>(), null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Search_ConditionsFromUrl_CappedAtApiLimit()
    {
        using var factory = new WebTestFactory();
        SetupReferences(factory);
        SetupSearch(factory);
        var url = "/search?" + string.Join("&", Enumerable.Range(0, 15).Select(i => $"factionConditions%5B{i}%5D.faction=ER"));
        var html = await GetHtml(factory, url);
        html.Should().Contain("Найдено");
        factory.Votes.Verify(v => v.SearchWithConditionsAsync(
            It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<DateTimeOffset?>(), It.IsAny<DateTimeOffset?>(),
            It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<int?>(), It.IsAny<int?>(),
            It.Is<IEnumerable<FactionConditionRequestApiModel>?>(c => c != null && c.Count() == 10),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Search_RendersAllStageAndOutcomeOptions()
    {
        using var factory = new WebTestFactory();
        SetupReferences(factory);
        SetupSearch(factory);
        var html = await GetHtml(factory, "/search");
        html.Should().Contain("отклонение законопроекта");
        html.Should().Contain("возврат ко второму чтению");
        html.Should().Contain("Неизвестно");
    }

    [Fact]
    public async Task Home_ApiUnavailable_RendersErrorAndPartialData()
    {
        using var factory = new WebTestFactory();
        factory.Stats.Setup(s => s.GetOverviewAsync()).ThrowsAsync(new HttpRequestException("connection refused"));
        factory.Convocations.Setup(c => c.GetAllAsync()).ReturnsAsync(TestData.Convocations());
        var html = await GetHtml(factory, "/");
        html.Should().Contain("Сервис поиска недоступен.");
        html.Should().Contain("VIII созыв");
    }

    [Fact]
    public async Task Home_ApiTimeout_RendersErrorAndPartialData()
    {
        using var factory = new WebTestFactory();
        factory.Stats.Setup(s => s.GetOverviewAsync()).ThrowsAsync(new TaskCanceledException("таймаут"));
        factory.Convocations.Setup(c => c.GetAllAsync()).ReturnsAsync(TestData.Convocations());
        var html = await GetHtml(factory, "/");
        html.Should().Contain("Сервис поиска недоступен.");
        html.Should().Contain("VIII созыв");
    }

    [Fact]
    public async Task Search_ApiTimeout_RendersError()
    {
        using var factory = new WebTestFactory();
        SetupReferences(factory);
        factory.Votes
            .Setup(v => v.SearchWithConditionsAsync(
                It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<DateTimeOffset?>(), It.IsAny<DateTimeOffset?>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<IEnumerable<FactionConditionRequestApiModel>?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TaskCanceledException("таймаут"));
        var html = await GetHtml(factory, "/search?q=" + Uri.EscapeDataString("алкоголь"));
        html.Should().Contain("Сервис поиска недоступен.");
    }

    [Fact]
    public async Task VoteCard_ApiTimeout_RendersError()
    {
        using var factory = new WebTestFactory();
        factory.Votes.Setup(v => v.GetDetailsAsync(126282)).ThrowsAsync(new TaskCanceledException("таймаут"));
        var html = await GetHtml(factory, "/votes/126282");
        html.Should().Contain("Сервис поиска недоступен.");
    }

    private static bool MatchesS2(IEnumerable<FactionConditionRequestApiModel> conditions)
    {
        var list = conditions.ToList();
        if (list.Count != 2)
        {
            return false;
        }

        return list[0].Faction == "NL" && list[0].Result == "accept" && list[0].Operator == "gte"
            && list[0].Threshold == 70 && list[0].Basis == "members"
            && list[1].Faction == "KPRF" && list[1].Result == "decline" && list[1].Operator == "gte"
            && list[1].Threshold == 50 && list[1].Basis == "members";
    }
}