using DumaLens.Api.Client;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace DumaLens.Web.Tests;

/// <summary>
/// Фабрика Web-хоста с моками клиентов Api (данные подменяются, HTTP к Api не выполняется)
/// </summary>
public sealed class WebTestFactory : WebApplicationFactory<Program>
{
    /// <summary>
    /// Мок клиента голосований
    /// </summary>
    public Mock<IVotesClient> Votes { get; } = new();

    /// <summary>
    /// Мок клиента законопроектов
    /// </summary>
    public Mock<IBillsClient> Bills { get; } = new();

    /// <summary>
    /// Мок клиента фракций
    /// </summary>
    public Mock<IFactionsClient> Factions { get; } = new();

    /// <summary>
    /// Мок клиента депутатов
    /// </summary>
    public Mock<IDeputiesClient> Deputies { get; } = new();

    /// <summary>
    /// Мок клиента созывов
    /// </summary>
    public Mock<IConvocationsClient> Convocations { get; } = new();

    /// <summary>
    /// Мок клиента сводки
    /// </summary>
    public Mock<IStatsClient> Stats { get; } = new();

    /// <summary>
    /// Подмена клиентов Api моками в контейнере хоста
    /// </summary>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IVotesClient>();
            services.RemoveAll<IBillsClient>();
            services.RemoveAll<IFactionsClient>();
            services.RemoveAll<IDeputiesClient>();
            services.RemoveAll<IConvocationsClient>();
            services.RemoveAll<IStatsClient>();
            services.AddSingleton(Votes.Object);
            services.AddSingleton(Bills.Object);
            services.AddSingleton(Factions.Object);
            services.AddSingleton(Deputies.Object);
            services.AddSingleton(Convocations.Object);
            services.AddSingleton(Stats.Object);
        });
    }
}