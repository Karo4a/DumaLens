using DumaLens.Entities.Contracts.Enums;

namespace DumaLens.Api.Models.Votes;

/// <summary>
/// Полная карточка голосования
/// </summary>
public class VoteDetailsApiModel : VoteApiModel
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
    public IReadOnlyCollection<DeputyVoteGroupApiModel> DeputyVoteGroups { get; set; } = [];
}