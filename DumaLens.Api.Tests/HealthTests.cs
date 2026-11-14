using DumaLens.Api.Tests.Infrastructure;
using FluentAssertions;
using System.Net;
using Xunit;

namespace DumaLens.Api.Tests;

/// <summary>
/// Тесты health-check хоста
/// </summary>
public class HealthTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory factory;

    /// <summary>
    /// Конструктор
    /// </summary>
    public HealthTests(TestWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    /// <summary>
    /// Health-check должен возвращать Healthy
    /// </summary>
    [Fact]
    public async Task HealthShouldReturnHealthy()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Be("Healthy");
    }
}