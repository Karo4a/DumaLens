namespace DumaLens.Importer.Crawl;

/// <summary>
/// Итоги обхода живого сайта голосований
/// </summary>
public sealed class CrawlSummary
{
    /// <summary>
    /// Обработано созывов
    /// </summary>
    public int ConvocationsProcessed { get; set; }

    /// <summary>
    /// Загружено страниц листингов
    /// </summary>
    public int ListingPagesFetched { get; set; }

    /// <summary>
    /// Просмотрено элементов листингов (голосований в списках)
    /// </summary>
    public int ListingItemsSeen { get; set; }

    /// <summary>
    /// Архивных голосований, у которых дообновлены итог/процент «За» из листинга
    /// </summary>
    public int OutcomeBackfilled { get; set; }

    /// <summary>
    /// Новых голосований загружено с сайта (Source = Site)
    /// </summary>
    public int NewVotes { get; set; }

    /// <summary>
    /// Карточек пропущено: голосование уже есть в базе (повторный запуск идемпотентен)
    /// </summary>
    public int CardsSkipped { get; set; }

    /// <summary>
    /// Идентификаторов отсутствует на сайте (устойчивый HTTP 500 после повторов)
    /// </summary>
    public int MissingIds { get; set; }

    /// <summary>
    /// Карточек отправлено в карантин (нарушения валидации; дедуп по SourceUrl)
    /// </summary>
    public int Quarantined { get; set; }

    /// <summary>
    /// Индивидуальных голосов депутатов записано через COPY
    /// </summary>
    public long DeputyVotesImported { get; set; }

    /// <summary>
    /// Фракционных агрегатов записано через COPY
    /// </summary>
    public long FactionStatsImported { get; set; }

    /// <summary>
    /// Пачек карточек обработано (каждая — одна транзакция)
    /// </summary>
    public int Batches { get; set; }

    /// <summary>
    /// Длительность обхода
    /// </summary>
    public TimeSpan Elapsed { get; set; }
}