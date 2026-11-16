using DumaLens.Common;
using DumaLens.Common.Contracts;
using DumaLens.Common.Extensions;
using DumaLens.Context;
using DumaLens.Context.Contracts;
using DumaLens.Importer.Crawl;
using DumaLens.Importer.Import;
using DumaLens.Importer.Stats;
using DumaLens.Importer.Verify;
using DumaLens.Parsing;
using DumaLens.Parsing.Contracts;
using DumaLens.Repositories;
using DumaLens.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace DumaLens.Importer;

/// <summary>
/// Сборка DI-контейнера импортёра (общая для CLI и интеграционных тестов)
/// </summary>
public static class ImporterHost
{
    /// <summary>
    /// Построить контейнер сервисов импортёра
    /// </summary>
    /// <param name="connectionString">Строка подключения PostgreSQL</param>
    /// <param name="logFilePath">Путь к файлу журнала; null — только stdout</param>
    public static ServiceProvider BuildServiceProvider(string connectionString, string? logFilePath)
    {
        var services = new ServiceCollection();
        services.AddDbContext<DumaLensContext>(options => options.UseNpgsql(
            connectionString,
            npgsqlOptions => npgsqlOptions.MigrationsAssembly(typeof(DumaLensContext).Assembly.FullName)));
        services.AddScoped<IReader>(provider => provider.GetRequiredService<DumaLensContext>());
        services.AddScoped<IWriter>(provider => provider.GetRequiredService<DumaLensContext>());
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<DumaLensContext>());
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.RegisterAssemblyInterfacesAssignableTo<IRepositoryAnchor>(ServiceLifetime.Scoped);
        services.RegisterAssemblyInterfacesAssignableTo<DumaLens.Services.IServiceAnchor>(ServiceLifetime.Scoped);
        services.AddSingleton(provider => new ImportLog(provider.GetRequiredService<IDateTimeProvider>(), logFilePath));
        services.AddScoped<IVoteArchiveParser, VoteArchiveParser>();
        services.AddScoped<IVoteListingParser, VoteListingParser>();
        services.AddScoped<IVotePageParser, VotePageParser>();
        services.AddScoped<ITitleNormalizer, TitleNormalizer>();
        services.AddScoped<IVoteParseValidator, VoteParseValidator>();
        services.AddScoped<ReferenceCatalog>();
        services.AddScoped<BulkLoadSchema>();
        services.AddScoped<ArchiveImporter>();
        services.AddScoped<ArchiveVerifier>();
        services.AddSingleton(provider =>
        {
            // Сайт отдаёт gzip (~17 КБ вместо ~156 КБ на карточку). UseCookies = false обязателен:
            // WAF сайта ограничивает скорость одновременных запросов с одной session-cookie до ~4 req/s
            // (замер: без cookie 24 req/s, с cookie 4,3); кроулеру сессия не нужна.
            // UseProxy = false — обход прямой, без системного прокси/WPAD.
            var handler = new HttpClientHandler
            {
                UseCookies = false,
                UseProxy = false,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
            };
            var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("DumaLens/1.0 (educational research crawler)");
            return client;
        });
        services.AddScoped<SiteFetcher>();
        services.AddScoped<RobotsChecker>();
        services.AddScoped<VoteCrawler>();
        services.AddScoped<StatsRecalculator>();
        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Определить строку подключения: явный параметр, затем переменная окружения ConnectionStrings__DefaultConnection
    /// </summary>
    public static string ResolveConnectionString(string? explicitConnection)
    {
        var connection = explicitConnection ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        if (string.IsNullOrWhiteSpace(connection))
        {
            throw new InvalidOperationException("Строка подключения не задана: укажите --connection или переменную окружения ConnectionStrings__DefaultConnection");
        }

        return connection;
    }
}