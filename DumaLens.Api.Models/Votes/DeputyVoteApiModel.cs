using DumaLens.Entities.Contracts.Enums;

namespace DumaLens.Api.Models.Votes;

/// <summary>
/// Голос депутата в карточке голосования
/// </summary>
public class DeputyVoteApiModel
{
    /// <summary>
    /// Идентификатор депутата на сайте Госдумы
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Полное имя депутата
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Результат голосования депутата
    /// </summary>
    public VoteResult Result { get; set; }
}