using DumaLens.Entities;
using DumaLens.Entities.Contracts.Enums;
using DumaLens.Importer.Crawl;
using DumaLens.Importer.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Text;

namespace DumaLens.Importer.Tests.Integration;

/// <summary>
/// Интеграционные тесты кроулера на локальном HTTP-сайте-фикстуре (Э7): добор итога у архивных
/// голосований, загрузка новых карточек, карантин, устойчивый 500, идемпотентность повторного обхода
/// </summary>
/// <remarks>
/// Каждый тест получает одноразовую базу PostgreSQL (ADR-3) и собственный локальный сервер
/// на свободном порту. Синтетические листинги/карточки повторяют разметку vote.duma.gov.ru.
/// </remarks>
public sealed class VoteCrawlerTests
{
    /// <summary>
    /// Обход VIII созыва: добор итога/процента у архивного голосования, новая пустая карточка,
    /// карантин бракованной карточки, отсутствующий id (устойчивый 500); существующая карточка не качается
    /// </summary>
    [SkippableFact]
    public async Task Crawl_LocalFixture_BackfillsOutcomeAndImportsNewCards()
    {
        var database = new DatabaseFixture();
        await database.InitializeAsync();
        Skip.If(database.SkipReason != null, database.SkipReason);
        await using var site = new LocalSiteFixture();
        try
        {
            await SeedExistingVote(database);
            AddSiteRoutes(site);
            await site.StartAsync();

            var summary = await RunCrawl(database.ConnectionString, site.BaseUrl);

            summary.ConvocationsProcessed.Should().Be(1);
            summary.ListingPagesFetched.Should().Be(2);
            summary.ListingItemsSeen.Should().Be(4);
            summary.CardsSkipped.Should().Be(1, "голосование 126282 уже в базе — карточка не перекачивается");
            summary.OutcomeBackfilled.Should().Be(1);
            summary.NewVotes.Should().Be(1);
            summary.MissingIds.Should().Be(1, "999999 отвечает устойчивым 500");
            summary.Quarantined.Should().Be(1, "777777 — 5 депутатов вне диапазона 430–460");
            summary.DeputyVotesImported.Should().Be(0);
            summary.FactionStatsImported.Should().Be(0);

            await using var context = database.CreateContext();
            var votes = await context.Set<Vote>().AsNoTracking().ToListAsync();
            votes.Should().HaveCount(2);

            var backfilled = votes.Single(x => x.SiteId == 126282);
            backfilled.Outcome.Should().Be(VoteOutcome.Accepted);
            backfilled.AcceptPercentSite.Should().Be(88.6m);
            backfilled.Source.Should().Be(VoteSource.Archive, "добор итога не меняет источник архивного голосования");

            var imported = votes.Single(x => x.SiteId == 888888);
            imported.Source.Should().Be(VoteSource.Site);
            imported.Outcome.Should().Be(VoteOutcome.Accepted);
            imported.AcceptPercentSite.Should().Be(89.0m);
            imported.HasResults.Should().BeFalse("пустая карточка deputiesData = [] — факт, не карантин");
            imported.Stage.Should().Be(VoteStage.ThirdReading);
            imported.BillId.Should().NotBeNull();

            var billNumber = await context.Set<Bill>().AsNoTracking()
                .Where(x => x.Id == imported.BillId)
                .Select(x => x.Number)
                .SingleAsync();
            billNumber.Should().Be("500091-8");

            var convocationNumber = await context.Set<Convocation>().AsNoTracking()
                .Where(x => x.Id == imported.ConvocationId)
                .Select(x => x.Number)
                .SingleAsync();
            convocationNumber.Should().Be(8);

            var quarantine = await context.Set<ImportQuarantine>().AsNoTracking().ToListAsync();
            quarantine.Should().ContainSingle();
            quarantine[0].SourceUrl.Should().Be($"{site.BaseUrl}/vote/777777");
            quarantine[0].SiteId.Should().Be(777777);
            quarantine[0].SourceFile.Should().BeNull("карантин сайта отличается от архивного источником");
            quarantine[0].Reason.Should().Contain("DeputyCountOutOfRange");

            site.Requests.Should().NotContain("/vote/126282");
            site.Requests.Should().Contain("/vote/888888");
            site.Requests.Should().Contain("/?convocation=AAAAAAA8&page=1");
            site.Requests.Should().Contain("/?convocation=AAAAAAA8&page=2");
        }
        finally
        {
            await database.DisposeAsync();
        }
    }

    /// <summary>
    /// Повторный обход идемпотентен: новые голоса/карантин не дублируются, добор итога не повторяется
    /// </summary>
    [SkippableFact]
    public async Task Crawl_SecondRun_IsIdempotent()
    {
        var database = new DatabaseFixture();
        await database.InitializeAsync();
        Skip.If(database.SkipReason != null, database.SkipReason);
        await using var site = new LocalSiteFixture();
        try
        {
            await SeedExistingVote(database);
            AddSiteRoutes(site);
            await site.StartAsync();

            var first = await RunCrawl(database.ConnectionString, site.BaseUrl);
            first.NewVotes.Should().Be(1);

            var second = await RunCrawl(database.ConnectionString, site.BaseUrl);

            second.NewVotes.Should().Be(0);
            second.OutcomeBackfilled.Should().Be(0);
            second.Quarantined.Should().Be(0, "карантин дедуплицируется по SourceUrl");
            second.CardsSkipped.Should().Be(2, "126282 и 888888 уже в базе");
            second.MissingIds.Should().Be(1);

            await using var context = database.CreateContext();
            (await context.Set<Vote>().AsNoTracking().CountAsync()).Should().Be(2);
            (await context.Set<DeputyVote>().AsNoTracking().CountAsync()).Should().Be(0);
            (await context.Set<VoteFactionStats>().AsNoTracking().CountAsync()).Should().Be(0);
            (await context.Set<ImportQuarantine>().AsNoTracking().CountAsync()).Should().Be(1);
        }
        finally
        {
            await database.DisposeAsync();
        }
    }

    /// <summary>
    /// --max-cards ограничивает число обрабатываемых карточек (пробные прогоны)
    /// </summary>
    [SkippableFact]
    public async Task Crawl_MaxCards_LimitsProcessedCards()
    {
        var database = new DatabaseFixture();
        await database.InitializeAsync();
        Skip.If(database.SkipReason != null, database.SkipReason);
        await using var site = new LocalSiteFixture();
        try
        {
            await SeedExistingVote(database);
            AddSiteRoutes(site);
            await site.StartAsync();

            var summary = await RunCrawl(database.ConnectionString, site.BaseUrl, maxCards: 1);

            summary.MissingIds.Should().Be(1, "первая карточка очереди — отсутствующий 999999");
            summary.NewVotes.Should().Be(0);
            summary.Quarantined.Should().Be(0);
            summary.Batches.Should().Be(1);

            await using var context = database.CreateContext();
            (await context.Set<Vote>().AsNoTracking().CountAsync()).Should().Be(1);
            site.Requests.Should().NotContain("/vote/888888");
            site.Requests.Should().NotContain("/vote/777777");
        }
        finally
        {
            await database.DisposeAsync();
        }
    }

    /// <summary>
    /// Параллельная загрузка карточек (--parallel) даёт тот же результат, что последовательная:
    /// состав импорта, карантин и пропуск существующих не зависят от параллельности
    /// </summary>
    [SkippableFact]
    public async Task Crawl_ParallelFetch_SameResultsAsSequential()
    {
        var database = new DatabaseFixture();
        await database.InitializeAsync();
        Skip.If(database.SkipReason != null, database.SkipReason);
        await using var site = new LocalSiteFixture();
        try
        {
            await SeedExistingVote(database);
            AddSiteRoutes(site);
            await site.StartAsync();

            var summary = await RunCrawl(database.ConnectionString, site.BaseUrl, maxCards: 0, parallel: 4);

            summary.NewVotes.Should().Be(1);
            summary.OutcomeBackfilled.Should().Be(1);
            summary.MissingIds.Should().Be(1);
            summary.Quarantined.Should().Be(1);
            summary.CardsSkipped.Should().Be(1);
            site.Requests.Should().Contain("/vote/888888");
            site.Requests.Should().Contain("/vote/999999");
            site.Requests.Should().Contain("/vote/777777");
            site.Requests.Should().NotContain("/vote/126282");

            await using var context = database.CreateContext();
            (await context.Set<Vote>().AsNoTracking().CountAsync()).Should().Be(2);
            (await context.Set<ImportQuarantine>().AsNoTracking().CountAsync()).Should().Be(1);
        }
        finally
        {
            await database.DisposeAsync();
        }
    }

    private static async Task<CrawlSummary> RunCrawl(string connectionString, string baseUrl, int maxCards = 0, int parallel = 1)
    {
        await using var provider = ImporterHost.BuildServiceProvider(connectionString, null);
        await using var scope = provider.CreateAsyncScope();
        var crawler = scope.ServiceProvider.GetRequiredService<VoteCrawler>();
        var options = new CrawlOptions
        {
            BaseUrl = baseUrl,
            Convocation = 8,
            FromPage = 1,
            DelayMs = 0,
            BatchSize = 2,
            MaxCards = maxCards,
            Parallel = parallel,
        };
        return await crawler.Crawl(options, CancellationToken.None);
    }

    private static async Task SeedExistingVote(DatabaseFixture database)
    {
        await using var context = database.CreateContext();
        var convocationId = await context.Set<Convocation>().AsNoTracking()
            .Where(x => x.Number == 8)
            .Select(x => x.Id)
            .SingleAsync();
        context.Set<Vote>().Add(new Vote
        {
            Id = Guid.NewGuid(),
            SiteId = 126282,
            Title = "(третье чтение) О проекте федерального закона № 500091-8 «О пробном голосовании»",
            VotedAt = new DateTimeOffset(2024, 3, 13, 12, 0, 0, TimeSpan.FromHours(3)),
            UtcOffsetMinutes = 180,
            ConvocationId = convocationId,
            Stage = VoteStage.ThirdReading,
            StageRaw = "третье чтение",
            HasResults = true,
            Outcome = VoteOutcome.Unknown,
            AcceptPercentSite = null,
            Source = VoteSource.Archive,
            AcceptCountTotal = 396,
            DeclineCountTotal = 0,
            AbstainCountTotal = 0,
            NoneCountTotal = 51,
        });
        await context.SaveChangesAsync();
    }

    private static void AddSiteRoutes(LocalSiteFixture site)
    {
        site.AddRoute("/robots.txt", "User-agent: *\nDisallow:\n");
        site.AddRoute("/?convocation=AAAAAAA8&page=1", BuildListingPage(
            activePage: 1,
            totalCount: 4,
            new ListingItem(126282, "green", "За: 396 чел (88.6%)"),
            new ListingItem(999999, "green", null),
            new ListingItem(888888, "green", "За: 400 чел (89.0%)")));
        site.AddRoute("/?convocation=AAAAAAA8&page=2", BuildListingPage(
            activePage: 2,
            totalCount: 4,
            new ListingItem(777777, "red", null)));
        site.AddRoute("/vote/999999", "internal error", status: 500);
        site.AddRoute("/vote/888888", BuildCard(
            "(третье чтение) О проекте федерального закона № 500091-8 «О новом голосовании»",
            "var deputiesData = [];"));
        site.AddRoute("/vote/777777", BuildCard(
            "(третье чтение) О проекте федерального закона № 100002-8 «О бракованной карточке»",
            BuildFiveDeputies()));
    }

    private static string BuildListingPage(int activePage, int totalCount, params ListingItem[] items)
    {
        var builder = new StringBuilder();
        builder.Append("<html><body>");
        builder.Append("<div class=\"date-head\">15.05.2024</div>");
        foreach (var item in items)
        {
            builder.Append("<div class=\"item\"><div class=\"item-left\">");
            builder.Append("<div class=\"time\">12:00:00</div>");
            builder.Append($"<a href=\"/vote/{item.SiteId}\">О проекте федерального закона</a>");
            builder.Append("</div><div class=\"item-right\">");
            builder.Append($"<div class=\"{item.OutcomeClass}\">{(item.OutcomeClass == "green" ? "Принят" : "Отклонён")}</div>");
            if (item.Counts != null)
            {
                builder.Append(item.Counts);
            }

            builder.Append("</div></div>");
        }

        builder.Append("<div class=\"page-nave\">");
        builder.Append($"<div class=\"page-nave-count\">Показаны: с 1 по 20 из {totalCount}</div>");
        for (var page = 1; page <= 2; page++)
        {
            if (page == activePage)
            {
                builder.Append($"<span class=\"page-nave-act\">{page}</span>");
            }
            else
            {
                builder.Append($"<a href=\"?convocation=AAAAAAA8&page={page}\">{page}</a>");
            }
        }

        builder.Append("</div></body></html>");
        return builder.ToString();
    }

    private static string BuildCard(string title, string deputiesDataJs)
    {
        return $"<html><body><h1>{title}</h1>"
            + "<div class=\"date-p\"><span>15.05.2024 12:00:00</span></div>"
            + "<a href=\"/?convocation=AAAAAAA8\">VIII созыв</a>"
            + $"<script>{deputiesDataJs}</script>"
            + "</body></html>";
    }

    private static string BuildFiveDeputies()
    {
        var builder = new StringBuilder("var deputiesData = [");
        string[] names = ["Иванов Иван Иванович", "Петров Пётр Петрович", "Сидоров Сидор Сидорович", "Кузнецов Кузьма Кузьмич", "Смирнов Сергей Сергеевич"];
        string[] results = ["for", "against", "abstain", "absent", "for"];
        for (var i = 0; i < names.Length; i++)
        {
            if (i > 0)
            {
                builder.Append(',');
            }

            builder.Append($"{{\"sortName\":\"{names[i]}\",\"url\":\"/deputy?deputy={i + 1}\",\"result\":\"{results[i]}\",\"factionCode\":\"{i + 1}\",\"faction\":\"Фракция {i + 1}\"}}");
        }

        builder.Append("];");
        return builder.ToString();
    }

    private sealed record ListingItem(long SiteId, string OutcomeClass, string? Counts);
}