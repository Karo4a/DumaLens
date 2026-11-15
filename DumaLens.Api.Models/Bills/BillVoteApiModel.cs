using DumaLens.Entities.Contracts.Enums;

namespace DumaLens.Api.Models.Bills;

/// <summary>
/// Голосование в хронологии законопроекта
/// </summary>
public class BillVoteApiModel
{
    /// <summary>
    /// Идентификатор голосования на сайте Госдумы
    /// </summary>
    public long Id { get; set; }

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
    public IReadOnlyCollection<BillFactionPositionApiModel> FactionPositions { get; set; } = [];
}