using System.Net;

namespace DumaLens.Importer.Tests.Infrastructure;

/// <summary>
/// Заглушка HttpMessageHandler: очередь заранее заданных ответов/сбоев для юнит-тестов кроулера
/// </summary>
public sealed class StubHttpHandler : HttpMessageHandler
{
    private readonly Queue<Func<Task<HttpResponseMessage>>> responses;

    /// <summary>
    /// Число выполненных запросов
    /// </summary>
    public int Calls { get; private set; }

    /// <summary>
    /// Конструктор
    /// </summary>
    public StubHttpHandler(params Func<Task<HttpResponseMessage>>[] responses)
    {
        this.responses = new Queue<Func<Task<HttpResponseMessage>>>(responses);
    }

    /// <summary>
    /// Ответ с заданным статусом и телом
    /// </summary>
    public static Func<Task<HttpResponseMessage>> Response(HttpStatusCode status, string body = "")
    {
        return () => Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body) });
    }

    /// <summary>
    /// Сбой запроса: исключение вместо ответа
    /// </summary>
    public static Func<Task<HttpResponseMessage>> Failure(Exception exception)
    {
        return () => Task.FromException<HttpResponseMessage>(exception);
    }

    /// <inheritdoc/>
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Calls++;
        var factory = responses.Count > 0
            ? responses.Dequeue()
            : Response(HttpStatusCode.OK, "ok");
        return factory();
    }
}