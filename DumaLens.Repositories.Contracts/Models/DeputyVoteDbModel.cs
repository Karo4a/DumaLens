using DumaLens.Entities.Contracts.Enums;

namespace DumaLens.Repositories.Contracts.Models;

/// <summary>
/// Голос депутата в карточке голосования
/// </summary>
public class DeputyVoteDbModel
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
    /// Имя депутата для сортировки (фамилия впереди)
    /// </summary>
    public string SortName { get; set; } = string.Empty;

    /// <summary>
    /// Код канонической фракции на момент голосования (null — без фракции)
    /// </summary>
    public string? FactionCode { get; set; }

    /// <summary>
    /// Историческое название фракции на момент голосования (null — без фракции)
    /// </summary>
    public string? FactionTitle { get; set; }

    /// <summary>
    /// Результат голосования депутата
    /// </summary>
    public VoteResult Result { get; set; }
}