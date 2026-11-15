using DumaLens.Api.Tests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace DumaLens.Api.Tests;

/// <summary>
/// Интеграционные тесты сводных показателей базы
/// </summary>
[Collection(nameof(DumaLensCollection))]
public class StatsApiTests
{
    private readonly HttpClient client;

    /// <summary>
    /// Конструктор
    /// </summary>
    public StatsApiTests(DumaLensApiFixture fixture)
    {
        client = fixture.CreateClient();
    }

    [SkippableFact]
    public async Task GetOverview_ReturnsCountersAndDateRange()
    {
        Skip.IfNot(PostgreSqlAvailability.IsAvailable, "PostgreSQL недоступен — интеграционный тест пропущен (ADR-3)");

        // Arrange
        // Act
        var root = await client.GetJsonAsync("/api/stats/overview");

        // Assert
        root.GetProperty("totalVotes").GetInt32().Should().Be(6);
        root.GetProperty("totalDeputies").GetInt32().Should().Be(4);
        root.GetProperty("totalFactions").GetInt32().Should().Be(25, "справочник фракций засеян миграцией InitialCreate");
        root.GetProperty("totalBills").GetInt32().Should().Be(2);
        root.GetProperty("dateFrom").GetString().Should().Be("2020-06-01");
        root.GetProperty("dateTo").GetString().Should().Be("2024-04-01");
    }
}