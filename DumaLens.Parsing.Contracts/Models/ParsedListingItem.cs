using DumaLens.Entities.Contracts.Enums;

namespace DumaLens.Parsing.Contracts.Models;

/// <summary>
/// Элемент листинга голосований сайта
/// </summary>
public sealed class ParsedListingItem
{
    /// <summary>
    /// Идентификатор голосования на сайте
    /// </summary>
    public long SiteId { get; set; }

    /// <summary>
    /// Очищенный заголовок голосования
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Дата и время голосования (дата группы + время элемента, московское время +03:00)
    /// </summary>
    public DateTimeOffset VotedAt { get; set; }

    /// <summary>
    /// Итог голосования по данным сайта
    /// </summary>
    public VoteOutcome Outcome { get; set; } = VoteOutcome.Unknown;

    /// <summary>
    /// Число голосов «за» с сайта (null, если не опубликовано)
    /// </summary>
    public int? AcceptCount { get; set; }

    /// <summary>
    /// Процент «за» с сайта (null, если не опубликован)
    /// </summary>
    public decimal? AcceptPercent { get; set; }

    /// <summary>
    /// Число голосов «против» с сайта (null, если не опубликовано)
    /// </summary>
    public int? DeclineCount { get; set; }

    /// <summary>
    /// Число голосов «воздержался» с сайта (null, если не опубликовано)
    /// </summary>
    public int? AbstainCount { get; set; }
}