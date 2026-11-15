using DumaLens.Api.Tests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace DumaLens.Api.Tests;

/// <summary>
/// Интеграционные тесты справочника созывов
/// </summary>
[Collection(nameof(DumaLensCollection))]
public class ConvocationsApiTests
{
    private readonly HttpClient client;

    /// <summary>
    /// Конструктор
    /// </summary>
    public ConvocationsApiTests(DumaLensApiFixture fixture)
    {
        client = fixture.CreateClient();
    }

    [SkippableFact]
    public async Task GetAll_ReturnsCatalogWithVoteCounts()
    {
        Skip.IfNot(PostgreSqlAvailability.IsAvailable, "PostgreSQL недоступен — интеграционный тест пропущен (ADR-3)");

        // Arrange
        // Act
        var root = await client.GetJsonAsync("/api/convocations");

        // Assert
        var convocations = root.EnumerateArray().ToList();
        convocations.Should().HaveCount(9, "справочник созывов засеян миграцией InitialCreate (Entities.Default)");
        var conv7 = convocations.Single(x => x.GetProperty("number").GetInt32() == 7);
        conv7.GetProperty("siteCode").GetString().Should().Be("AAAAAAA7");
        conv7.GetProperty("voteCount").GetInt32().Should().Be(1);
        var conv8 = convocations.Single(x => x.GetProperty("number").GetInt32() == 8);
        conv8.GetProperty("voteCount").GetInt32().Should().Be(5);
    }
}