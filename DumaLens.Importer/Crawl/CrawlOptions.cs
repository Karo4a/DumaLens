namespace DumaLens.Importer.Crawl;

/// <summary>
/// Параметры обхода живого сайта голосований (Э7)
/// </summary>
public sealed class CrawlOptions
{
    /// <summary>
    /// Базовый URL сайта голосований
    /// </summary>
    public string BaseUrl { get; set; } = "https://vote.duma.gov.ru";

    /// <summary>
    /// Номер созыва 1–9; 0 — все созывы по возрастанию номера
    /// </summary>
    public int Convocation { get; set; }

    /// <summary>
    /// Начальная страница листинга (возобновление прерванного обхода)
    /// </summary>
    public int FromPage { get; set; } = 1;

    /// <summary>
    /// Пауза между запросами к сайту в миллисекундах (вежливость; рекомендуется ≥ 500)
    /// </summary>
    public int DelayMs { get; set; } = 1000;

    /// <summary>
    /// Размер пачки карточек в одной транзакции
    /// </summary>
    public int BatchSize { get; set; } = 100;

    /// <summary>
    /// Ограничение числа обрабатываемых карточек (0 — без ограничения; для пробных прогонов)
    /// </summary>
    public int MaxCards { get; set; }

    /// <summary>
    /// Число одновременных запросов карточек (1 — последовательно; рекомендуется не более 8)
    /// </summary>
    public int Parallel { get; set; } = 1;
}