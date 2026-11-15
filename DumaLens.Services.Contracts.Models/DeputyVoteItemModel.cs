using DumaLens.Entities.Contracts.Enums;

namespace DumaLens.Services.Contracts.Models;

/// <summary>
/// Голос депутата в карточке голосования
/// </summary>
public class DeputyVoteItemModel
{
    /// <summary>
    /// Идентификатор депутата на сайте Госдумы
    /// </summary>
    public long DeputySiteId { get; set; }

    /// <summary>
    /// Полное имя депутата
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Результат голосования депутата
    /// </summary>
    public VoteResult Result { get; set; }
}