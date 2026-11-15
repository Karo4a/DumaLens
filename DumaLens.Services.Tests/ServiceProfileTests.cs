using AutoMapper;
using DumaLens.Services.Infrastructure;
using Xunit;

namespace DumaLens.Services.Tests;

/// <summary>
/// Проверка конфигурации профилей автомаппинга сервиса (Э5): все члены моделей назначения
/// покрыты источником — незамеченное поле контракта не уходит в default молча
/// </summary>
public sealed class ServiceProfileTests
{
    /// <summary>
    /// Конфигурация ServiceProfile валидна (незамапленных членов назначения нет)
    /// </summary>
    [Fact]
    public void ServiceProfileConfigurationShouldBeValid()
    {
        var configuration = new MapperConfiguration(profile => profile.AddProfile<ServiceProfile>());

        configuration.AssertConfigurationIsValid();
    }
}