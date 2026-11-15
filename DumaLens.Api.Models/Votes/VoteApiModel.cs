using DumaLens.Entities.Contracts.Enums;

namespace DumaLens.Api.Models.Votes;

/// <summary>
/// Голосование в результатах поиска
/// </summary>
public class VoteApiModel
{
    /// <summary>
    /// Идентификатор голосования на сайте Госдумы
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Нормализованный заголовок голосования
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Время голосования с историческим московским смещением
    /// </summary>
    public DateTimeOffset VotedAt { get; set; }

    /// <summary>
    /// Номер созыва
    /// </summary>
    public int Convocation { get; set; }

    /// <summary>
    /// Номер законопроекта (null — голосование не по законопроекту)
    /// </summary>
    public string? BillNumber { get; set; }

    /// <summary>
    /// Стадия рассмотрения
    /// </summary>
    public VoteStage Stage { get; set; }

    /// <summary>
    /// Итог голосования
    /// </summary>
    public VoteOutcome Outcome { get; set; }

    /// <summary>
    /// Число голосов «за»
    /// </summary>
    public int AcceptCount { get; set; }

    /// <summary>
    /// Число голосов «против»
    /// </summary>
    public int DeclineCount { get; set; }

    /// <summary>
    /// Число голосов «воздержался»
    /// </summary>
    public int AbstainCount { get; set; }

    /// <summary>
    /// Число голосов «не голосовал»
    /// </summary>
    public int NoneCount { get; set; }

    /// <summary>
    /// Есть ли результаты голосования
    /// </summary>
    public bool HasResults { get; set; }

    /// <summary>
    /// Статистика по фракциям
    /// </summary>
    public IReadOnlyCollection<VoteFactionStatsApiModel> FactionStats { get; set; } = [];
}