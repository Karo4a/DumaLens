using DumaLens.Entities.Contracts.Enums;

namespace DumaLens.Services.Contracts.Models;

/// <summary>
/// Голосование в хронологии законопроекта
/// </summary>
public class BillVoteModel
{
    /// <summary>
    /// Идентификатор голосования на сайте Госдумы
    /// </summary>
    public long SiteId { get; set; }

    /// <summary>
    /// Время голосования с историческим московским смещением
    /// </summary>
    public DateTimeOffset VotedAt { get; set; }

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
    public IReadOnlyCollection<BillFactionPositionModel> FactionPositions { get; set; } = [];
}