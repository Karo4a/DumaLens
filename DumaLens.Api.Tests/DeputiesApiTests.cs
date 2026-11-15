using DumaLens.Api.Tests.Infrastructure;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace DumaLens.Api.Tests;

/// <summary>
/// Интеграционные тесты карточек депутатов и списков депутатов созывов
/// </summary>
[Collection(nameof(DumaLensCollection))]
public class DeputiesApiTests
{
    private readonly DumaLensApiFixture fixture;
    private readonly HttpClient client;

    /// <summary>
    /// Конструктор
    /// </summary>
    public DeputiesApiTests(DumaLensApiFixture fixture)
    {
        this.fixture = fixture;
        client = fixture.CreateClient();
    }

    [SkippableFact]
    public async Task GetByConvocation_ReturnsDeputiesWithFactions()
    {
        Skip.IfNot(PostgreSqlAvailability.IsAvailable, "PostgreSQL недоступен — интеграционный тест пропущен (ADR-3)");

        // Arrange
        // Act
        var root = await client.GetJsonAsync("/api/deputies?convocation=8");

        // Assert
        var deputies = root.EnumerateArray().ToList();
        deputies.Should().HaveCount(3, "депутат без фракции в список созыва не попадает");
        deputies[0].GetProperty("fullName").GetString().Should().Be("Иванов Иван Иванович");
        deputies[0].GetProperty("factionCodes").EnumerateArray().Select(x => x.GetString()).Should().BeEquivalentTo(new[] { "ER" });
        deputies[1].GetProperty("fullName").GetString().Should().Be("Петров Пётр Петрович");
        deputies[2].GetProperty("fullName").GetString().Should().Be("Сидоров Сидор Сидорович");
    }

    [SkippableFact]
    public async Task GetByConvocation_EmptyConvocation_ReturnsEmptyList()
    {
        Skip.IfNot(PostgreSqlAvailability.IsAvailable, "PostgreSQL недоступен — интеграционный тест пропущен (ADR-3)");

        // Arrange
        // Act
        var root = await client.GetJsonAsync("/api/deputies?convocation=9");

        // Assert
        root.EnumerateArray().Should().BeEmpty();
    }

    [SkippableTheory]
    [InlineData("/api/deputies?convocation=0")]
    [InlineData("/api/deputies?convocation=10")]
    [InlineData("/api/deputies")]
    public async Task GetByConvocation_InvalidConvocation_Returns422(string url)
    {
        Skip.IfNot(PostgreSqlAvailability.IsAvailable, "PostgreSQL недоступен — интеграционный тест пропущен (ADR-3)");

        // Arrange
        // Act
        var response = await client.GetAsync(url);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var root = await response.Content.ReadFromJsonAsync<JsonDocument>();
        root!.RootElement.GetProperty("errors").EnumerateArray().Should().NotBeEmpty();
    }

    [SkippableFact]
    public async Task GetCard_ReturnsMembershipsAndCounters()
    {
        Skip.IfNot(PostgreSqlAvailability.IsAvailable, "PostgreSQL недоступен — интеграционный тест пропущен (ADR-3)");

        // Arrange
        var deputy = fixture.Seed.Deputy1;

        // Act
        var root = await client.GetJsonAsync($"/api/deputies/{deputy.SiteId}");

        // Assert
        root.GetProperty("id").GetInt64().Should().Be(deputy.SiteId);
        root.GetProperty("fullName").GetString().Should().Be("Иванов Иван Иванович");
        var factions = root.GetProperty("factions").EnumerateArray().ToList();
        factions.Should().HaveCount(2);
        factions.Select(x => x.GetProperty("convocation").GetInt32()).Should().BeEquivalentTo(new[] { 7, 8 });
        factions.Should().OnlyContain(x => x.GetProperty("factionCode").GetString() == "ER");
        root.GetProperty("totalVotes").GetInt32().Should().Be(2);
        root.GetProperty("acceptCount").GetInt32().Should().Be(2);
        root.GetProperty("participationPct").GetDecimal().Should().Be(100m);
    }

    [SkippableFact]
    public async Task GetCard_DeputyWithoutFaction_ParticipationZero()
    {
        Skip.IfNot(PostgreSqlAvailability.IsAvailable, "PostgreSQL недоступен — интеграционный тест пропущен (ADR-3)");

        // Arrange
        var deputy = fixture.Seed.Deputy4;

        // Act
        var root = await client.GetJsonAsync($"/api/deputies/{deputy.SiteId}");

        // Assert
        root.GetProperty("factions").EnumerateArray().Should().BeEmpty();
        root.GetProperty("totalVotes").GetInt32().Should().Be(2);
        root.GetProperty("noneCount").GetInt32().Should().Be(2);
        root.GetProperty("participationPct").GetDecimal().Should().Be(0m);
    }

    [SkippableFact]
    public async Task GetCard_UnknownId_Returns404()
    {
        Skip.IfNot(PostgreSqlAvailability.IsAvailable, "PostgreSQL недоступен — интеграционный тест пропущен (ADR-3)");

        // Arrange
        // Act
        var response = await client.GetAsync("/api/deputies/999999999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var root = await response.Content.ReadFromJsonAsync<JsonDocument>();
        root!.RootElement.GetProperty("message").GetString().Should().Contain("999999999");
    }
}