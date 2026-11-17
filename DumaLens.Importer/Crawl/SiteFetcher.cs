using DumaLens.Importer.Import;
using System.Net;

namespace DumaLens.Importer.Crawl;

/// <summary>
/// Загрузка страниц сайта с паузой вежливости и повторами при сбоях (Э7)
/// </summary>
/// <remarks>
/// HTTP 404 — страница отсутствует (без повторов); 5xx и сетевые сбои — до <see cref="RetryCount"/>
/// повторов с экспоненциальной задержкой (DelayMs × 2, 4, 8, не менее 500 мс). Устойчивый 500 у карточки
/// означает «id отсутствует на сайте» (особенность vote.duma.gov.ru). Отмена токена
/// пробрасывается наружу, а не глотается как сетевой сбой.
/// </remarks>
public sealed class SiteFetcher
{
    /// <summary>
    /// Число повторов после первой неудачной попытки
    /// </summary>
    public const int RetryCount = 3;

    // Нижняя граница задержки повтора: при DelayMs = 0 повторы не должны идти мгновенно
    private const int MinBackoffMs = 500;

    private readonly HttpClient httpClient;
    private readonly ImportLog log;

    /// <summary>
    /// Конструктор
    /// </summary>
    public SiteFetcher(HttpClient httpClient, ImportLog log)
    {
        this.httpClient = httpClient;
        this.log = log;
    }

    /// <summary>
    /// Загрузить страницу; null — страница отсутствует (404) или устойчиво недоступна (5xx/сеть после повторов)
    /// </summary>
    public async Task<string?> FetchAsync(string url, int delayMs, CancellationToken cancellationToken)
    {
        for (var attempt = 0; ; attempt++)
        {
            if (delayMs > 0)
            {
                await Task.Delay(delayMs, cancellationToken);
            }

            try
            {
                using var response = await httpClient.GetAsync(url, cancellationToken);
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return null;
                }

                if ((int)response.StatusCode >= 500)
                {
                    if (attempt < RetryCount)
                    {
                        await Backoff(url, $"HTTP {(int)response.StatusCode}", attempt, delayMs, cancellationToken);
                        continue;
                    }

                    log.Error($"Устойчивый HTTP {(int)response.StatusCode} для {url} после {RetryCount} повторов");
                    return null;
                }

                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync(cancellationToken);
            }
            catch (Exception ex) when (ex is HttpRequestException || (ex is TaskCanceledException && !cancellationToken.IsCancellationRequested))
            {
                if (attempt < RetryCount)
                {
                    await Backoff(url, ex.GetType().Name, attempt, delayMs, cancellationToken);
                    continue;
                }

                log.Error($"Запрос {url} не удался после {RetryCount} повторов: {ex.Message}");
                return null;
            }
        }
    }

    private async Task Backoff(string url, string reason, int attempt, int delayMs, CancellationToken cancellationToken)
    {
        var backoffMs = Math.Max(delayMs * (1 << (attempt + 1)), MinBackoffMs);
        log.Info($"Сбой запроса {url} ({reason}) — повтор {attempt + 1}/{RetryCount} через {backoffMs} мс");
        if (backoffMs > 0)
        {
            await Task.Delay(backoffMs, cancellationToken);
        }
    }
}