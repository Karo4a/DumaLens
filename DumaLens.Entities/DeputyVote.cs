using DumaLens.Entities.Contracts.Enums;

namespace DumaLens.Entities;

/// <summary>
/// Индивидуальный голос депутата в голосовании
/// </summary>
/// <remarks>
/// Потоковая таблица без аудита и soft-delete (ADR-2): составной ключ (VoteId, DeputyId), данные иммутабельны.
/// </remarks>
public class DeputyVote
{
    /// <summary>
    /// Идентификатор голосования
    /// </summary>
    public Guid VoteId { get; set; }

    /// <summary>
    /// Идентификатор депутата
    /// </summary>
    public Guid DeputyId { get; set; }

    /// <summary>
    /// Идентификатор фракционного периода депутата на момент голосования (null, если не установлен)
    /// </summary>
    public Guid? FactionPeriodId { get; set; }

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