using DumaLens.Common;
using DumaLens.Importer.Crawl;
using DumaLens.Importer.Import;
using DumaLens.Importer.Tests.Infrastructure;
using FluentAssertions;
using System.Net;

namespace DumaLens.Importer.Tests;

/// <summary>
/// Юнит-тесты загрузки страниц сайта: повторы при 5xx/сетевых сбоях, 404 без повторов,
/// устойчивый 500 после исчерпания попыток (Э7)
/// </summary>
public sealed class SiteFetcherTests
{
    /// <summary>
    /// Временные 5xx: повторы с задержкой, затем успешный ответ
    /// </summary>
    [Fact]
    public async Task Fetch_Transient5xx_RetriesThenSucceeds()
    {
        var handler = new StubHttpHandler(
            StubHttpHandler.Response(HttpStatusCode.InternalServerError),
            StubHttpHandler.Response(HttpStatusCode.ServiceUnavailable),
            StubHttpHandler.Response(HttpStatusCode.OK, "ok"));
        var fetcher = CreateFetcher(handler);

        var html = await fetcher.FetchAsync("http://localhost/vote/1", delayMs: 0, CancellationToken.None);

        html.Should().Be("ok");
        handler.Calls.Should().Be(3);
    }

    /// <summary>
    /// Устойчивый 500: null после первой попытки и трёх повторов («id отсутствует на сайте»)
    /// </summary>
    [Fact]
    public async Task Fetch_Persistent500_ReturnsNullAfterRetries()
    {
        var handler = new StubHttpHandler(
            StubHttpHandler.Response(HttpStatusCode.InternalServerError),
            StubHttpHandler.Response(HttpStatusCode.InternalServerError),
            StubHttpHandler.Response(HttpStatusCode.InternalServerError),
            StubHttpHandler.Response(HttpStatusCode.InternalServerError));
        var fetcher = CreateFetcher(handler);

        var html = await fetcher.FetchAsync("http://localhost/vote/999999", delayMs: 0, CancellationToken.None);

        html.Should().BeNull();
        handler.Calls.Should().Be(SiteFetcher.RetryCount + 1);
    }

    /// <summary>
    /// 404: страница отсутствует, повторы не выполняются
    /// </summary>
    [Fact]
    public async Task Fetch_404_ReturnsNullWithoutRetries()
    {
        var handler = new StubHttpHandler(StubHttpHandler.Response(HttpStatusCode.NotFound));
        var fetcher = CreateFetcher(handler);

        var html = await fetcher.FetchAsync("http://localhost/vote/1", delayMs: 0, CancellationToken.None);

        html.Should().BeNull();
        handler.Calls.Should().Be(1);
    }

    /// <summary>
    /// Таймаут запроса: повтор, затем успешный ответ (отмена токена не глотается)
    /// </summary>
    [Fact]
    public async Task Fetch_Timeout_RetriesThenSucceeds()
    {
        var handler = new StubHttpHandler(
            StubHttpHandler.Failure(new TaskCanceledException()),
            StubHttpHandler.Failure(new TaskCanceledException()),
            StubHttpHandler.Response(HttpStatusCode.OK, "ok"));
        var fetcher = CreateFetcher(handler);

        var html = await fetcher.FetchAsync("http://localhost/vote/1", delayMs: 0, CancellationToken.None);

        html.Should().Be("ok");
        handler.Calls.Should().Be(3);
    }

    /// <summary>
    /// Сетевой сбой (HttpRequestException): повтор, затем успешный ответ
    /// </summary>
    [Fact]
    public async Task Fetch_NetworkError_RetriesThenSucceeds()
    {
        var handler = new StubHttpHandler(
            StubHttpHandler.Failure(new HttpRequestException("connection reset")),
            StubHttpHandler.Response(HttpStatusCode.OK, "ok"));
        var fetcher = CreateFetcher(handler);

        var html = await fetcher.FetchAsync("http://localhost/vote/1", delayMs: 0, CancellationToken.None);

        html.Should().Be("ok");
        handler.Calls.Should().Be(2);
    }

    /// <summary>
    /// Отмена пользовательского токена пробрасывается наружу, а не глотается как сетевой сбой
    /// </summary>
    [Fact]
    public async Task Fetch_UserCancellation_Propagates()
    {
        var handler = new StubHttpHandler(StubHttpHandler.Response(HttpStatusCode.OK, "ok"));
        var fetcher = CreateFetcher(handler);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = () => fetcher.FetchAsync("http://localhost/vote/1", delayMs: 0, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        handler.Calls.Should().BeLessThanOrEqualTo(1, "повторы не выполняются: отмена токена пробрасывается, а не глотается как сетевой сбой");
    }

    private static SiteFetcher CreateFetcher(StubHttpHandler handler)
    {
        var httpClient = new HttpClient(handler);
        var log = new ImportLog(new DateTimeProvider(), null);
        return new SiteFetcher(httpClient, log);
    }
}