using AutoMapper;
using DumaLens.Api.Infrastructure;
using DumaLens.Services.Infrastructure;
using Xunit;

namespace DumaLens.Api.Tests;

/// <summary>
/// Тесты профиля маппинга API
/// </summary>
public class ApiMapperTests
{
    [Fact]
    public void ConfigurationShouldBeValid()
    {
        // Arrange
        var configuration = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<ServiceProfile>();
            cfg.AddProfile<ApiMapper>();
        });

        // Act
        // Assert
        configuration.AssertConfigurationIsValid();
    }
}