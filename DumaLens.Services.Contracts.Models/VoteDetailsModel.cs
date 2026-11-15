using DumaLens.Entities.Contracts.Enums;

namespace DumaLens.Services.Contracts.Models;

/// <summary>
/// Полная карточка голосования
/// </summary>
public class VoteDetailsModel : VoteSearchItemModel
{
    /// <summary>
    /// Исходная формулировка стадии с сайта (null для архива)
    /// </summary>
    public string? StageRaw { get; set; }

    /// <summary>
    /// Процент «за» по данным сайта (null для архива)
    /// </summary>
    public decimal? AcceptPercentSite { get; set; }

    /// <summary>
    /// Источник данных голосования
    /// </summary>
    public VoteSource Source { get; set; }

    /// <summary>
    /// Голоса депутатов, сгруппированные по фракциям
    /// </summary>
    public IReadOnlyCollection<DeputyVoteGroupModel> DeputyVoteGroups { get; set; } = [];
}