using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace DumaLens.Api.Tests;

/// <summary>
/// Хелпер настройки тестового хоста
/// </summary>
internal static class WebHostBuilderHelper
{
    /// <summary>
    /// Добавляет конфигурацию интеграционных тестов и устанавливает окружение
    /// </summary>
    public static void ConfigureTestAppConfiguration(this IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            var projectDir = Directory.GetCurrentDirectory();
            var configPath = Path.Combine(projectDir, "appsettings.integration.json");
            config.AddJsonFile(configPath).AddEnvironmentVariables();
        });
        builder.UseEnvironment(EnvironmentProvider.IntegrationEnvironment);
    }
}