using DumaLens.Entities.Contracts.Enums;

namespace DumaLens.Entities;

/// <summary>
/// Индивидуальный голос депутата в голосовании
/// </summary>
/// <remarks>
/// Потоковая таблица без аудита и soft-delete (ADR-2): составной ключ (VoteId, DeputyId), данные иммутабельны.
/// Внешние ключи ссылаются на узкие целочисленные RowId справочников (ADR-14);
/// GUID справочников остаются первичными ключами.
/// </remarks>
public class DeputyVote
{
    /// <summary>
    /// Узкий ключ голосования (Votes.RowId)
    /// </summary>
    public int VoteId { get; set; }

    /// <summary>
    /// Узкий ключ депутата (Deputies.RowId)
    /// </summary>
    public int DeputyId { get; set; }

    /// <summary>
    /// Узкий ключ фракционного периода депутата на момент голосования (FactionPeriods.RowId; null, если не установлен)
    /// </summary>
    public short? FactionPeriodId { get; set; }

    /// <summary>
    /// Результат голоса
    /// </summary>
    public VoteResult Result { get; set; }

    /// <summary>
    /// Голосование
    /// </summary>
    public Vote Vote { get; set; } = null!;

    /// <summary>
    /// Депутат
    /// </summary>
    public Deputy Deputy { get; set; } = null!;

    /// <summary>
    /// Фракционный период депутата на момент голосования
    /// </summary>
    public FactionPeriod? FactionPeriod { get; set; }
}