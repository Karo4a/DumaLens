namespace DumaLens.Importer.Stats;

/// <summary>
/// Итог полного пересчёта фракционных агрегатов
/// </summary>
public sealed class RecalcSummary
{
    /// <summary>
    /// Количество построенных строк VoteFactionStats
    /// </summary>
    public long RowsRebuilt { get; init; }

    /// <summary>
    /// Длительность пересчёта
    /// </summary>
    public TimeSpan Elapsed { get; init; }
}