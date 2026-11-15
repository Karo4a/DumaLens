using DumaLens.Api.Tests.Infrastructure;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace DumaLens.Api.Tests;

/// <summary>
/// Интеграционные тесты карточек законопроектов и поиска по части номера
/// </summary>
[Collection(nameof(DumaLensCollection))]
public class BillsApiTests
{
    private readonly DumaLensApiFixture fixture;
    private readonly HttpClient client;

    /// <summary>
    /// Конструктор
    /// </summary>
    public BillsApiTests(DumaLensApiFixture fixture)
    {
        this.fixture = fixture;
        client = fixture.CreateClient();
    }

    [SkippableFact]
    public async Task Search_ByNumberPart_ReturnsBills()
    {
        Skip.IfNot(PostgreSqlAvailability.IsAvailable, "PostgreSQL недоступен — интеграционный тест пропущен (ADR-3)");

        // Arrange
        // Act
        var root = await client.GetJsonAsync("/api/bills?numberPart=5000");

        // Assert
        var items = root.EnumerateArray().ToList();
        items.Should().ContainSingle();
        items[0].GetProperty("number").GetString().Should().Be("500091-8");
        items[0].GetProperty("sozdUrl").GetString().Should().Be("https://sozd.duma.gov.ru/bill/500091-8");
    }

    [SkippableTheory]
    [InlineData("/api/bills?numberPart=")]
    [InlineData("/api/bills")]
    public async Task Search_EmptyNumberPart_Returns422(string url)
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
    public async Task GetCard_ReturnsChronologyWithFactionPositions()
    {
        Skip.IfNot(PostgreSqlAvailability.IsAvailable, "PostgreSQL недоступен — интеграционный тест пропущен (ADR-3)");

        // Arrange
        // Act
        var root = await client.GetJsonAsync("/api/bills/500091-8");

        // Assert
        root.GetProperty("number").GetString().Should().Be("500091-8");
        root.GetProperty("sozdUrl").GetString().Should().Be("https://sozd.duma.gov.ru/bill/500091-8");
        var votes = root.GetProperty("votes").EnumerateArray().ToList();
        votes.Should().HaveCount(2);
        votes[0].GetProperty("id").GetInt64().Should().Be(fixture.Seed.AlcoholFirst.SiteId, "хронология — от раннего к позднему");
        votes[0].GetProperty("stage").GetString().Should().Be("FirstReading");
        votes[1].GetProperty("id").GetInt64().Should().Be(fixture.Seed.AlcoholThird.SiteId);
        var positions = votes[1].GetProperty("factionPositions").EnumerateArray().ToList();
        positions.Should().HaveCount(3);
        var er = positions.Single(x => x.GetProperty("factionCode").GetString() == "ER");
        er.GetProperty("acceptPctMembers").GetDecimal().Should().Be(100m);
    }

    [SkippableFact]
    public async Task GetCard_UnknownNumber_Returns404()
    {
        Skip.IfNot(PostgreSqlAvailability.IsAvailable, "PostgreSQL недоступен — интеграционный тест пропущен (ADR-3)");

        // Arrange
        // Act
        var response = await client.GetAsync("/api/bills/000000-0");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var root = await response.Content.ReadFromJsonAsync<JsonDocument>();
        root!.RootElement.GetProperty("message").GetString().Should().Contain("000000-0");
    }
}