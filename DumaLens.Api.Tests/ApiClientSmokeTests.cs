using DumaLens.Api.Client;
using DumaLens.Api.Tests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace DumaLens.Api.Tests;

/// <summary>
/// Смоук-тесты NSwag-клиента: сгенерированные клиенты ходят в тестовый хост по HTTP
/// </summary>
[Collection(nameof(DumaLensCollection))]
public class ApiClientSmokeTests
{
    private readonly DumaLensApiFixture fixture;

    /// <summary>
    /// Конструктор
    /// </summary>
    public ApiClientSmokeTests(DumaLensApiFixture fixture)
    {
        this.fixture = fixture;
    }

    [SkippableFact]
    public async Task VotesClient_SearchAndDetails_RoundTrip()
    {
        Skip.IfNot(PostgreSqlAvailability.IsAvailable, "PostgreSQL недоступен — интеграционный тест пропущен (ADR-3)");

        // Arrange
        var client = new VotesClient("", fixture.CreateClient());

        // Act
        var search = await client.SearchAsync("алкоголь", null, null, null, null, null, null, null, null, null, null, CancellationToken.None);
        var thirdReading = search.Items.Single(x => x.Stage == VoteStage.ThirdReading);
        var details = await client.GetDetailsAsync(thirdReading.Id, CancellationToken.None);

        // Assert
        search.TotalCount.Should().Be(2, "по законопроекту 500091-8 в сиде два голосования: второе и третье чтение");
        details.Title.Should().Contain("500091-8");
        details.Stage.Should().Be(VoteStage.ThirdReading);
        details.BillNumber.Should().Be("500091-8");
        details.DeputyVoteGroups.Should().NotBeEmpty();
        details.FactionStats.Should().NotBeEmpty();
    }

    [SkippableFact]
    public async Task BillsClient_GetCard_ReturnsChronology()
    {
        Skip.IfNot(PostgreSqlAvailability.IsAvailable, "PostgreSQL недоступен — интеграционный тест пропущен (ADR-3)");

        // Arrange
        var client = new BillsClient("", fixture.CreateClient());

        // Act
        var card = await client.GetCardAsync("500091-8", CancellationToken.None);

        // Assert
        card.Number.Should().Be("500091-8");
        card.SozdUrl.Should().Be("https://sozd.duma.gov.ru/bill/500091-8");
        card.Votes.Should().HaveCount(2);
    }

    [SkippableFact]
    public async Task StatsAndFactionsClients_ReturnOverviewAndCard()
    {
        Skip.IfNot(PostgreSqlAvailability.IsAvailable, "PostgreSQL недоступен — интеграционный тест пропущен (ADR-3)");

        // Arrange
        var httpClient = fixture.CreateClient();
        var statsClient = new StatsClient("", httpClient);
        var factionsClient = new FactionsClient("", httpClient);

        // Act
        var overview = await statsClient.GetOverviewAsync(CancellationToken.None);
        var faction = await factionsClient.GetCardAsync("ER", CancellationToken.None);

        // Assert
        overview.TotalVotes.Should().Be(6);
        overview.TotalDeputies.Should().Be(4);
        faction.Code.Should().Be("ER");
        faction.Periods.Should().HaveCount(7);
        faction.ConvocationStats.Should().HaveCount(2);
    }
}