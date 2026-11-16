using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace DumaLens.Importer.Tests.Infrastructure;

/// <summary>
/// Локальный HTTP-сервер сайта-фикстуры для интеграционных тестов кроулера (Э7)
/// </summary>
/// <remarks>
/// Маршруты задаются словарём «путь+запрос → (статус, тело)»; неизвестные пути отвечают 404.
/// Все запросы журналируются в <see cref="Requests"/> — проверка пропуска уже импортированных
/// карточек и состава обхода. Сервер слушает только loopback на свободном порту.
/// </remarks>
public sealed class LocalSiteFixture : IAsyncDisposable
{
    private readonly Dictionary<string, SiteRoute> routes = new(StringComparer.Ordinal);
    private WebApplication? app;

    /// <summary>
    /// Базовый URL запущенного сервера
    /// </summary>
    public string BaseUrl { get; private set; } = string.Empty;

    /// <summary>
    /// Журнал поступивших запросов (путь + строка запроса)
    /// </summary>
    public ConcurrentQueue<string> Requests { get; } = new();

    /// <summary>
    /// Добавить маршрут «путь+запрос → тело ответа»
    /// </summary>
    public void AddRoute(string pathAndQuery, string body, int status = 200)
    {
        routes[pathAndQuery] = new SiteRoute(status, body);
    }

    /// <summary>
    /// Запустить сервер на свободном порту loopback
    /// </summary>
    public async Task StartAsync()
    {
        var port = GetFreePort();
        var builder = WebApplication.CreateSlimBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls($"http://127.0.0.1:{port}");
        app = builder.Build();
        app.MapFallback(async context =>
        {
            var key = context.Request.Path + context.Request.QueryString;
            Requests.Enqueue(key);
            if (routes.TryGetValue(key, out var route))
            {
                context.Response.StatusCode = route.Status;
                context.Response.ContentType = "text/html; charset=utf-8";
                await context.Response.WriteAsync(route.Body);
            }
            else
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
            }
        });
        await app.StartAsync();
        BaseUrl = $"http://127.0.0.1:{port}";
    }

    /// <inheritdoc cref="IAsyncDisposable.DisposeAsync"/>
    public async ValueTask DisposeAsync()
    {
        if (app != null)
        {
            await app.StopAsync();
            await app.DisposeAsync();
        }
    }

    private static int GetFreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private sealed record SiteRoute(int Status, string Body);
}