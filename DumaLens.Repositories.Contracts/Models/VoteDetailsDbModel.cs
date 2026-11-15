using DumaLens.Entities.Contracts.Enums;

namespace DumaLens.Repositories.Contracts.Models;

/// <summary>
/// Полная карточка голосования для чтения из БД
/// </summary>
public class VoteDetailsDbModel : VoteSearchDbModel
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
    /// Голоса депутатов с фракционной принадлежностью
    /// </summary>
    public IReadOnlyCollection<DeputyVoteDbModel> DeputyVotes { get; set; } = [];
}