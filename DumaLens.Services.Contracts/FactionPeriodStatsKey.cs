namespace DumaLens.Services.Contracts;

/// <summary>
/// Сопоставление узкого ключа фракционного периода (FactionPeriods.RowId) с идентификаторами
/// периода и канонической фракции для построения фракционных агрегатов
/// </summary>
public sealed class FactionPeriodStatsKey
{
    /// <summary>
    /// Идентификатор фракционного периода
    /// </summary>
    public Guid PeriodId { get; }

    /// <summary>
    /// Идентификатор канонической фракции
    /// </summary>
    public Guid FactionId { get; }

    /// <summary>
    /// Конструктор
    /// </summary>
    public FactionPeriodStatsKey(Guid periodId, Guid factionId)
    {
        PeriodId = periodId;
        FactionId = factionId;
    }
}