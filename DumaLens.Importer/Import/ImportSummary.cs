namespace DumaLens.Importer.Import;

/// <summary>
/// Итоговые счётчики импорта архива
/// </summary>
public sealed class ImportSummary
{
    /// <summary>
    /// Всего файлов голосований в архиве
    /// </summary>
    public int TotalFiles { get; set; }

    /// <summary>
    /// Импортировано (создано или обновлено) голосований
    /// </summary>
    public long Imported { get; set; }

    /// <summary>
    /// Пропущено уже обработанных голосований в режиме resume
    /// </summary>
    public long Skipped { get; set; }

    /// <summary>
    /// Отправлено в карантин голосований
    /// </summary>
    public long Quarantined { get; set; }

    /// <summary>
    /// Записано индивидуальных голосов депутатов
    /// </summary>
    public long DeputyVotesImported { get; set; }

    /// <summary>
    /// Записано строк фракционной статистики (VoteFactionStats)
    /// </summary>
    public long FactionStatsImported { get; set; }

    /// <summary>
    /// Обработано пачек
    /// </summary>
    public int Batches { get; set; }

    /// <summary>
    /// Длительность импорта
    /// </summary>
    public TimeSpan Elapsed { get; set; }
}