using System.Text;

namespace DumaLens.Api.Client;

/// <summary>
/// Заглушки для методов PrepareRequestAsync и ProcessResponseAsync клиента BillsClient
/// </summary>
public partial class BillsClient
{
    /// <summary>
    /// Вызывается перед отправкой HTTP-запроса (с финальным URL)
    /// </summary>
    protected virtual Task PrepareRequestAsync(HttpClient client, HttpRequestMessage request, string url, CancellationToken cancellationToken)
        => Task.CompletedTask;

    /// <summary>
    /// Вызывается перед отправкой HTTP-запроса (с построителем URL)
    /// </summary>
    protected virtual Task PrepareRequestAsync(HttpClient client, HttpRequestMessage request, StringBuilder urlBuilder, CancellationToken cancellationToken)
        => Task.CompletedTask;

    /// <summary>
    /// Вызывается после получения HTTP-ответа
    /// </summary>
    protected virtual Task ProcessResponseAsync(HttpClient client, HttpResponseMessage response, CancellationToken cancellationToken)
        => Task.CompletedTask;
}

/// <summary>
/// Заглушки для методов PrepareRequestAsync и ProcessResponseAsync клиента ConvocationsClient
/// </summary>
public partial class ConvocationsClient
{
    /// <summary>
    /// Вызывается перед отправкой HTTP-запроса (с финальным URL)
    /// </summary>
    protected virtual Task PrepareRequestAsync(HttpClient client, HttpRequestMessage request, string url, CancellationToken cancellationToken)
        => Task.CompletedTask;

    /// <summary>
    /// Вызывается перед отправкой HTTP-запроса (с построителем URL)
    /// </summary>
    protected virtual Task PrepareRequestAsync(HttpClient client, HttpRequestMessage request, StringBuilder urlBuilder, CancellationToken cancellationToken)
        => Task.CompletedTask;

    /// <summary>
    /// Вызывается после получения HTTP-ответа
    /// </summary>
    protected virtual Task ProcessResponseAsync(HttpClient client, HttpResponseMessage response, CancellationToken cancellationToken)
        => Task.CompletedTask;
}

/// <summary>
/// Заглушки для методов PrepareRequestAsync и ProcessResponseAsync клиента DeputiesClient
/// </summary>
public partial class DeputiesClient
{
    /// <summary>
    /// Вызывается перед отправкой HTTP-запроса (с финальным URL)
    /// </summary>
    protected virtual Task PrepareRequestAsync(HttpClient client, HttpRequestMessage request, string url, CancellationToken cancellationToken)
        => Task.CompletedTask;

    /// <summary>
    /// Вызывается перед отправкой HTTP-запроса (с построителем URL)
    /// </summary>
    protected virtual Task PrepareRequestAsync(HttpClient client, HttpRequestMessage request, StringBuilder urlBuilder, CancellationToken cancellationToken)
        => Task.CompletedTask;

    /// <summary>
    /// Вызывается после получения HTTP-ответа
    /// </summary>
    protected virtual Task ProcessResponseAsync(HttpClient client, HttpResponseMessage response, CancellationToken cancellationToken)
        => Task.CompletedTask;
}

/// <summary>
/// Заглушки для методов PrepareRequestAsync и ProcessResponseAsync клиента FactionsClient
/// </summary>
public partial class FactionsClient
{
    /// <summary>
    /// Вызывается перед отправкой HTTP-запроса (с финальным URL)
    /// </summary>
    protected virtual Task PrepareRequestAsync(HttpClient client, HttpRequestMessage request, string url, CancellationToken cancellationToken)
        => Task.CompletedTask;

    /// <summary>
    /// Вызывается перед отправкой HTTP-запроса (с построителем URL)
    /// </summary>
    protected virtual Task PrepareRequestAsync(HttpClient client, HttpRequestMessage request, StringBuilder urlBuilder, CancellationToken cancellationToken)
        => Task.CompletedTask;

    /// <summary>
    /// Вызывается после получения HTTP-ответа
    /// </summary>
    protected virtual Task ProcessResponseAsync(HttpClient client, HttpResponseMessage response, CancellationToken cancellationToken)
        => Task.CompletedTask;
}

/// <summary>
/// Заглушки для методов PrepareRequestAsync и ProcessResponseAsync клиента StatsClient
/// </summary>
public partial class StatsClient
{
    /// <summary>
    /// Вызывается перед отправкой HTTP-запроса (с финальным URL)
    /// </summary>
    protected virtual Task PrepareRequestAsync(HttpClient client, HttpRequestMessage request, string url, CancellationToken cancellationToken)
        => Task.CompletedTask;

    /// <summary>
    /// Вызывается перед отправкой HTTP-запроса (с построителем URL)
    /// </summary>
    protected virtual Task PrepareRequestAsync(HttpClient client, HttpRequestMessage request, StringBuilder urlBuilder, CancellationToken cancellationToken)
        => Task.CompletedTask;

    /// <summary>
    /// Вызывается после получения HTTP-ответа
    /// </summary>
    protected virtual Task ProcessResponseAsync(HttpClient client, HttpResponseMessage response, CancellationToken cancellationToken)
        => Task.CompletedTask;
}

/// <summary>
/// Заглушки для методов PrepareRequestAsync и ProcessResponseAsync клиента VotesClient
/// </summary>
public partial class VotesClient
{
    /// <summary>
    /// Вызывается перед отправкой HTTP-запроса (с финальным URL)
    /// </summary>
    protected virtual Task PrepareRequestAsync(HttpClient client, HttpRequestMessage request, string url, CancellationToken cancellationToken)
        => Task.CompletedTask;

    /// <summary>
    /// Вызывается перед отправкой HTTP-запроса (с построителем URL)
    /// </summary>
    protected virtual Task PrepareRequestAsync(HttpClient client, HttpRequestMessage request, StringBuilder urlBuilder, CancellationToken cancellationToken)
        => Task.CompletedTask;

    /// <summary>
    /// Вызывается после получения HTTP-ответа
    /// </summary>
    protected virtual Task ProcessResponseAsync(HttpClient client, HttpResponseMessage response, CancellationToken cancellationToken)
        => Task.CompletedTask;
}