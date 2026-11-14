using AutoMapper;
using DumaLens.Api.Tests.Infrastructure;
using DumaLens.Common.Contracts;
using DumaLens.Context;
using DumaLens.Context.Contracts;
using DumaLens.Services.Contracts;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using Xunit;

namespace DumaLens.Api.Tests;

/// <summary>
/// Тесты зависимостей
/// </summary>
public class DependenciesTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory factory;

    /// <summary>
    /// Конструктор
    /// </summary>
    public DependenciesTests(TestWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    /// <summary>
    /// Проверка резолва контроллеров (на Э0 контроллеров нет, цикл заработает с Э6)
    /// </summary>
    [Fact]
    public void ControllerCoreShouldBeResolved()
    {
        foreach (var controller in GetControllers())
        {
            // Arrange
            using var scope = factory.Services.CreateScope();

            // Act
            var instance = scope.ServiceProvider.GetRequiredService(controller);

            // Assert
            instance.Should().NotBeNull();
        }
    }

    /// <summary>
    /// Проверка резолва базовых сервисов
    /// </summary>
    [Fact]
    public void CoreServicesShouldBeResolved()
    {
        // Arrange
        using var scope = factory.Services.CreateScope();

        // Act
        var reader = scope.ServiceProvider.GetRequiredService<IReader>();
        var writer = scope.ServiceProvider.GetRequiredService<IWriter>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var dateTimeProvider = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();
        var validateService = scope.ServiceProvider.GetRequiredService<IValidateService>();
        var context = scope.ServiceProvider.GetRequiredService<DumaLensContext>();

        // Assert
        reader.Should().NotBeNull();
        writer.Should().NotBeNull();
        unitOfWork.Should().NotBeNull();
        dateTimeProvider.Should().NotBeNull();
        mapper.Should().NotBeNull();
        validateService.Should().NotBeNull();
        context.Should().NotBeNull();
    }

    private static IReadOnlyList<Type> GetControllers()
    {
        var assembly = Assembly.GetAssembly(typeof(Program));
        if (assembly == null)
        {
            return Array.Empty<Type>();
        }
        return assembly.DefinedTypes
            .Where(type => typeof(ControllerBase).IsAssignableFrom(type) && !type.IsAbstract)
            .ToList();
    }
}