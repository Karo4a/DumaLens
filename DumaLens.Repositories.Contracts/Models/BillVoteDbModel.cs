using DumaLens.Entities.Contracts.Enums;

namespace DumaLens.Repositories.Contracts.Models;

/// <summary>
/// Голосование в хронологии законопроекта
/// </summary>
public class BillVoteDbModel
{
    /// <summary>
    /// Идентификатор голосования
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Идентификатор голосования на сайте Госдумы
    /// </summary>
    public long SiteId { get; set; }

    /// <summary>
    /// Мгновенное время голосования (UTC)
    /// </summary>
    public DateTimeOffset VotedAt { get; set; }

    /// <summary>
    /// Историческое смещение московского времени в минутах
    /// </summary>
    public int UtcOffsetMinutes { get; set; }

    /// <summary>
    /// Стадия рассмотрения
    /// </summary>
    public VoteStage Stage { get; set; }

    /// <summary>
    /// Итог голосования
    /// </summary>
    public VoteOutcome Outcome { get; set; }

    /// <summary>
    /// Позиции фракций по голосованию
    /// </summary>
    public IReadOnlyCollection<BillFactionPositionDbModel> FactionPositions { get; set; } = [];
}