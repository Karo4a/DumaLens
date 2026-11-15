using DumaLens.Api.Tests.Infrastructure;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace DumaLens.Api.Tests;

/// <summary>
/// Интеграционные тесты канонических фракций и карточек фракций
/// </summary>
[Collection(nameof(DumaLensCollection))]
public class FactionsApiTests
{
    private readonly HttpClient client;

    /// <summary>
    /// Конструктор
    /// </summary>
    public FactionsApiTests(DumaLensApiFixture fixture)
    {
        client = fixture.CreateClient();
    }

    [SkippableFact]
    public async Task GetAll_ReturnsCanonicalFactionsWithPeriods()
    {
        Skip.IfNot(PostgreSqlAvailability.IsAvailable, "PostgreSQL недоступен — интеграционный тест пропущен (ADR-3)");

        // Arrange
        // Act
        var root = await client.GetJsonAsync("/api/factions");

        // Assert
        var factions = root.EnumerateArray().ToList();
        factions.Should().HaveCount(25, "справочник фракций засеян миграцией InitialCreate (Entities.Default)");
        var er = factions.Single(x => x.GetProperty("code").GetString() == "ER");
        er.GetProperty("title").GetString().Should().Be("Единая Россия");
        var periods = er.GetProperty("periods").EnumerateArray().ToList();
        periods.Should().HaveCount(7);
        periods.Select(x => x.GetProperty("convocation").GetInt32()).Should().BeEquivalentTo(new[] { 3, 3, 4, 5, 6, 7, 8 });
        er.GetProperty("convocationStats").EnumerateArray().Should().BeEmpty("сводная статистика заполняется только в карточке");
    }

    [SkippableFact]
    public async Task GetCard_ReturnsPeriodsAndConvocationStats()
    {
        Skip.IfNot(PostgreSqlAvailability.IsAvailable, "PostgreSQL недоступен — интеграционный тест пропущен (ADR-3)");

        // Arrange
        // Act
        var root = await client.GetJsonAsync("/api/factions/ER");

        // Assert
        root.GetProperty("code").GetString().Should().Be("ER");
        root.GetProperty("periods").EnumerateArray().Should().HaveCount(7);
        var stats = root.GetProperty("convocationStats").EnumerateArray().ToList();
        stats.Should().HaveCount(2);
        var conv7 = stats.Single(x => x.GetProperty("convocation").GetInt32() == 7);
        conv7.GetProperty("voteCount").GetInt32().Should().Be(1);
        conv7.GetProperty("avgAcceptPctMembers").GetDecimal().Should().Be(71.43m);
        var conv8 = stats.Single(x => x.GetProperty("convocation").GetInt32() == 8);
        conv8.GetProperty("voteCount").GetInt32().Should().Be(3);
        conv8.GetProperty("avgAcceptPctMembers").GetDecimal().Should().Be(83.33m, "среднее из 100, 100 и 50");
    }

    [SkippableFact]
    public async Task GetCard_UnknownCode_Returns404()
    {
        Skip.IfNot(PostgreSqlAvailability.IsAvailable, "PostgreSQL недоступен — интеграционный тест пропущен (ADR-3)");

        // Arrange
        // Act
        var response = await client.GetAsync("/api/factions/NOPE");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var root = await response.Content.ReadFromJsonAsync<JsonDocument>();
        root!.RootElement.GetProperty("message").GetString().Should().Contain("NOPE");
    }
}