namespace DumaLens.Entities;

/// <summary>
/// Предрасчитанная фракционная статистика голосования
/// </summary>
/// <remarks>
/// Потоковая таблица без аудита и soft-delete (ADR-2, ADR-7): составной ключ (VoteId, FactionPeriodId).
/// Проценты — numeric(5,2), null при нулевом знаменателе; базы: от списочного состава и от голосовавших.
/// База «от голосовавших» — только для accept/decline/abstain: «не голосовал» в неё не входит (Docs/04 §1.2).
/// </remarks>
public class VoteFactionStats
{
    /// <summary>
    /// Идентификатор голосования
    /// </summary>
    public Guid VoteId { get; set; }

    /// <summary>
    /// Идентификатор фракционного периода
    /// </summary>
    public Guid FactionPeriodId { get; set; }

    /// <summary>
    /// Идентификатор канонической фракции (денормализация для фильтра по канону)
    /// </summary>
    public Guid FactionId { get; set; }

    /// <summary>
    /// Количество голосов «за»
    /// </summary>
    public int AcceptCount { get; set; }

    /// <summary>
    /// Количество голосов «против»
    /// </summary>
    public int DeclineCount { get; set; }

    /// <summary>
    /// Количество голосов «воздержался»
    /// </summary>
    public int AbstainCount { get; set; }

    /// <summary>
    /// Количество голосов «не голосовал»
    /// </summary>
    public int NoneCount { get; set; }

    /// <summary>
    /// Списочный состав фракции на момент голосования
    /// </summary>
    public int MemberCount { get; set; }

    /// <summary>
    /// Количество голосовавших (за + против + воздержался)
    /// </summary>
    public int VotedCount { get; set; }

    /// <summary>
    /// Процент «за» от списочного состава
    /// </summary>
    public decimal? AcceptPctMembers { get; set; }

    /// <summary>
    /// Процент «против» от списочного состава
    /// </summary>
    public decimal? DeclinePctMembers { get; set; }

    /// <summary>
    /// Процент «воздержался» от списочного состава
    /// </summary>
    public decimal? AbstainPctMembers { get; set; }

    /// <summary>
    /// Процент «не голосовал» от списочного состава
    /// </summary>
    public decimal? NonePctMembers { get; set; }

    /// <summary>
    /// Процент «за» от голосовавших
    /// </summary>
    public decimal? AcceptPctVoted { get; set; }

    /// <summary>
    /// Процент «против» от голосовавших
    /// </summary>
    public decimal? DeclinePctVoted { get; set; }

    /// <summary>
    /// Процент «воздержался» от голосовавших
    /// </summary>
    public decimal? AbstainPctVoted { get; set; }

    /// <summary>
    /// Голосование
    /// </summary>
    public Vote Vote { get; set; } = null!;

    /// <summary>
    /// Фракционный период
    /// </summary>
    public FactionPeriod FactionPeriod { get; set; } = null!;

    /// <summary>
    /// Каноническая фракция
    /// </summary>
    public Faction Faction { get; set; } = null!;
}