using DumaLens.Api.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DumaLens.Api.Tests;

/// <summary>
/// Дымовой тест одноразовой базы PostgreSQL (ADR-3)
/// </summary>
[Collection(nameof(DumaLensCollection))]
public class DatabaseSmokeTests
{
    private readonly DumaLensApiFixture fixture;

    /// <summary>
    /// Конструктор
    /// </summary>
    public DatabaseSmokeTests(DumaLensApiFixture fixture)
    {
        this.fixture = fixture;
    }

    /// <summary>
    /// Одноразовая база должна быть доступна
    /// </summary>
    [SkippableFact]
    public async Task DatabaseShouldBeCreated()
    {
        Skip.IfNot(PostgreSqlAvailability.IsAvailable, "PostgreSQL недоступен — интеграционный тест пропущен (ADR-3)");

        // Arrange
        // Act
        var canConnect = await fixture.Context.Database.CanConnectAsync();

        // Assert
        canConnect.Should().BeTrue();
    }
}