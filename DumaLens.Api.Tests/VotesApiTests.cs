using DumaLens.Api.Tests.Infrastructure;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace DumaLens.Api.Tests;

/// <summary>
/// Интеграционные тесты поиска голосований и карточек голосований
/// </summary>
[Collection(nameof(DumaLensCollection))]
public class VotesApiTests
{
    private readonly DumaLensApiFixture fixture;
    private readonly HttpClient client;

    /// <summary>
    /// Конструктор
    /// </summary>
    public VotesApiTests(DumaLensApiFixture fixture)
    {
        this.fixture = fixture;
        client = fixture.CreateClient();
    }

    [SkippableFact]
    public async Task Search_WithoutFilters_ReturnsAllVotes()
    {
        Skip.IfNot(PostgreSqlAvailability.IsAvailable, "PostgreSQL недоступен — интеграционный тест пропущен (ADR-3)");

        // Arrange
        // Act
        var root = await client.GetJsonAsync("/api/votes");

        // Assert
        root.GetProperty("totalCount").GetInt32().Should().Be(6);
        root.GetProperty("page").GetInt32().Should().Be(1);
        root.GetProperty("pageSize").GetInt32().Should().Be(20);
        var items = root.GetProperty("items").EnumerateArray().ToList();
        items.Should().HaveCount(6);
        items[0].GetProperty("id").GetInt64().Should().Be(fixture.Seed.EmptyVote.SiteId, "сортировка по умолчанию — дата по убыванию");
        items[0].GetProperty("stage").GetString().Should().Be("Unknown");
        items[0].GetProperty("hasResults").GetBoolean().Should().BeFalse();
    }

    [SkippableFact]
    public async Task Search_ByTextQuery_FindsAlcoholVotes()
    {
        Skip.IfNot(PostgreSqlAvailability.IsAvailable, "PostgreSQL недоступен — интеграционный тест пропущен (ADR-3)");

        // Arrange
        var url = "/api/votes?q=" + Uri.EscapeDataString("алкогольной");

        // Act
        var root = await client.GetJsonAsync(url);

        // Assert
        root.GetProperty("totalCount").GetInt32().Should().Be(2);
        foreach (var item in root.GetProperty("items").EnumerateArray())
        {
            item.GetProperty("billNumber").GetString().Should().Be("500091-8");
        }
    }

    [SkippableFact]
    public async Task Search_ByConvocation_FiltersVotes()
    {
        Skip.IfNot(PostgreSqlAvailability.IsAvailable, "PostgreSQL недоступен — интеграционный тест пропущен (ADR-3)");

        // Arrange
        // Act
        var root = await client.GetJsonAsync("/api/votes?convocation=7");

        // Assert
        root.GetProperty("totalCount").GetInt32().Should().Be(1);
        var item = root.GetProperty("items")[0];
        item.GetProperty("id").GetInt64().Should().Be(fixture.Seed.BudgetVote.SiteId);
        item.GetProperty("convocation").GetInt32().Should().Be(7);
    }

    [SkippableFact]
    public async Task Search_ByBillNumber_FiltersVotes()
    {
        Skip.IfNot(PostgreSqlAvailability.IsAvailable, "PostgreSQL недоступен — интеграционный тест пропущен (ADR-3)");

        // Arrange
        // Act
        var root = await client.GetJsonAsync("/api/votes?billNumber=500091-8");

        // Assert
        root.GetProperty("totalCount").GetInt32().Should().Be(2);
    }

    [SkippableFact]
    public async Task Search_ByDateRange_FiltersVotes()
    {
        Skip.IfNot(PostgreSqlAvailability.IsAvailable, "PostgreSQL недоступен — интеграционный тест пропущен (ADR-3)");

        // Arrange
        // Act
        var root = await client.GetJsonAsync("/api/votes?dateFrom=2024-01-01&dateTo=2024-03-31");

        // Assert
        root.GetProperty("totalCount").GetInt32().Should().Be(3);
    }

    [SkippableFact]
    public async Task Search_ByStageAndOutcome_FiltersVotes()
    {
        Skip.IfNot(PostgreSqlAvailability.IsAvailable, "PostgreSQL недоступен — интеграционный тест пропущен (ADR-3)");

        // Arrange
        // Act
        var root = await client.GetJsonAsync("/api/votes?stage=ThirdReading&outcome=Accepted");

        // Assert
        root.GetProperty("totalCount").GetInt32().Should().Be(2);
    }

    [SkippableFact]
    public async Task Search_SortDateAsc_OrdersByDate()
    {
        Skip.IfNot(PostgreSqlAvailability.IsAvailable, "PostgreSQL недоступен — интеграционный тест пропущен (ADR-3)");

        // Arrange
        // Act
        var root = await client.GetJsonAsync("/api/votes?sort=dateAsc");

        // Assert
        root.GetProperty("items")[0].GetProperty("id").GetInt64().Should().Be(fixture.Seed.BudgetVote.SiteId);
    }

    [SkippableFact]
    public async Task Search_Pagination_ReturnsRequestedPage()
    {
        Skip.IfNot(PostgreSqlAvailability.IsAvailable, "PostgreSQL недоступен — интеграционный тест пропущен (ADR-3)");

        // Arrange
        // Act
        var root = await client.GetJsonAsync("/api/votes?sort=dateAsc&page=2&pageSize=2");

        // Assert
        root.GetProperty("totalCount").GetInt32().Should().Be(6);
        root.GetProperty("page").GetInt32().Should().Be(2);
        root.GetProperty("pageSize").GetInt32().Should().Be(2);
        var items = root.GetProperty("items").EnumerateArray().ToList();
        items.Should().HaveCount(2);
        items[0].GetProperty("id").GetInt64().Should().Be(fixture.Seed.ProcedureVote.SiteId);
        items[1].GetProperty("id").GetInt64().Should().Be(fixture.Seed.BudgetSecond.SiteId);
    }

    [SkippableFact]
    public async Task Search_FactionConditions_CombineAnd()
    {
        Skip.IfNot(PostgreSqlAvailability.IsAvailable, "PostgreSQL недоступен — интеграционный тест пропущен (ADR-3)");

        // Arrange
        var url = "/api/votes?factionConditions[0].faction=ER&factionConditions[0].result=accept&factionConditions[0].operator=gte&factionConditions[0].threshold=100&factionConditions[0].basis=members"
            + "&factionConditions[1].faction=KPRF&factionConditions[1].result=decline&factionConditions[1].operator=gte&factionConditions[1].threshold=50&factionConditions[1].basis=members";

        // Act
        var root = await client.GetJsonAsync(url);

        // Assert
        root.GetProperty("totalCount").GetInt32().Should().Be(2);
    }

    [SkippableFact]
    public async Task Search_FactionConditionByHistoricalTitle_ResolvesCanonicalFaction()
    {
        Skip.IfNot(PostgreSqlAvailability.IsAvailable, "PostgreSQL недоступен — интеграционный тест пропущен (ADR-3)");

        // Arrange
        var url = "/api/votes?factionConditions[0].faction=" + Uri.EscapeDataString("Всероссийская политическая партия «ЕДИНАЯ РОССИЯ»")
            + "&factionConditions[0].result=accept&factionConditions[0].operator=gte&factionConditions[0].threshold=70&factionConditions[0].basis=members";

        // Act
        var root = await client.GetJsonAsync(url);

        // Assert
        root.GetProperty("totalCount").GetInt32().Should().Be(3, "историческое название резолвится в каноническую фракцию во всех созывах: 100, 100 и 71.43 процента");
        var ids = root.GetProperty("items").EnumerateArray().Select(x => x.GetProperty("id").GetInt64()).ToList();
        ids.Should().Contain(fixture.Seed.BudgetVote.SiteId, "голосование седьмого созыва с историческим названием периода тоже найдено");
    }

    [SkippableTheory]
    [InlineData("/api/votes?stage=NotAStage")]
    [InlineData("/api/votes?pageSize=101")]
    [InlineData("/api/votes?page=0")]
    [InlineData("/api/votes?dateFrom=2024-01-02&dateTo=2024-01-01")]
    [InlineData("/api/votes?sort=relevance")]
    [InlineData("/api/votes?factionConditions[0].faction=ER&factionConditions[0].result=none&factionConditions[0].basis=voted&factionConditions[0].threshold=0")]
    [InlineData("/api/votes?factionConditions[0].faction=ER&factionConditions[0].result=accept&factionConditions[0].threshold=150")]
    public async Task Search_InvalidQuery_Returns422(string url)
    {
        Skip.IfNot(PostgreSqlAvailability.IsAvailable, "PostgreSQL недоступен — интеграционный тест пропущен (ADR-3)");

        // Arrange
        // Act
        var response = await client.GetAsync(url);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var root = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var errors = root!.RootElement.GetProperty("errors").EnumerateArray().ToList();
        errors.Should().NotBeEmpty();
        errors[0].GetProperty("message").GetString().Should().NotBeNullOrEmpty();
    }

    [SkippableFact]
    public async Task GetDetails_ReturnsFullCard()
    {
        Skip.IfNot(PostgreSqlAvailability.IsAvailable, "PostgreSQL недоступен — интеграционный тест пропущен (ADR-3)");

        // Arrange
        var vote = fixture.Seed.AlcoholThird;

        // Act
        var root = await client.GetJsonAsync($"/api/votes/{vote.SiteId}");

        // Assert
        root.GetProperty("id").GetInt64().Should().Be(vote.SiteId);
        root.GetProperty("votedAt").GetString().Should().Be("2024-03-13T15:01:13+03:00");
        root.GetProperty("convocation").GetInt32().Should().Be(8);
        root.GetProperty("billNumber").GetString().Should().Be("500091-8");
        root.GetProperty("stage").GetString().Should().Be("ThirdReading");
        root.GetProperty("outcome").GetString().Should().Be("Accepted");
        root.GetProperty("source").GetString().Should().Be("Archive");
        root.GetProperty("acceptCount").GetInt32().Should().Be(3);
        root.GetProperty("declineCount").GetInt32().Should().Be(1);
        root.GetProperty("noneCount").GetInt32().Should().Be(1);
        root.GetProperty("hasResults").GetBoolean().Should().BeTrue();

        var factionStats = root.GetProperty("factionStats").EnumerateArray().ToList();
        factionStats.Should().HaveCount(3);
        var er = factionStats.Single(x => x.GetProperty("factionCode").GetString() == "ER");
        er.GetProperty("memberCount").GetInt32().Should().Be(2);
        er.GetProperty("acceptPctMembers").GetDecimal().Should().Be(100m);

        var groups = root.GetProperty("deputyVoteGroups").EnumerateArray().ToList();
        groups.Should().HaveCount(4);
        groups[0].GetProperty("factionCode").GetString().Should().Be("ER");
        groups[1].GetProperty("factionCode").GetString().Should().Be("KPRF");
        groups[2].GetProperty("factionCode").GetString().Should().Be("NL");
        groups[3].GetProperty("factionCode").ValueKind.Should().Be(JsonValueKind.Null, "депутаты без фракции — последняя группа");
        var erDeputies = groups[0].GetProperty("deputies").EnumerateArray().ToList();
        erDeputies.Should().ContainSingle();
        erDeputies[0].GetProperty("id").GetInt64().Should().Be(fixture.Seed.Deputy1.SiteId);
        erDeputies[0].GetProperty("result").GetString().Should().Be("Accept");
        var independent = groups[3].GetProperty("deputies").EnumerateArray().ToList();
        independent[0].GetProperty("result").GetString().Should().Be("None");
    }

    [SkippableFact]
    public async Task GetDetails_UnknownId_Returns404()
    {
        Skip.IfNot(PostgreSqlAvailability.IsAvailable, "PostgreSQL недоступен — интеграционный тест пропущен (ADR-3)");

        // Arrange
        // Act
        var response = await client.GetAsync("/api/votes/999999999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var root = await response.Content.ReadFromJsonAsync<JsonDocument>();
        root!.RootElement.GetProperty("message").GetString().Should().Contain("999999999");
    }
}