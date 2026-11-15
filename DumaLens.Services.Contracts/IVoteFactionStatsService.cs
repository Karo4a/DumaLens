using DumaLens.Entities;

namespace DumaLens.Services.Contracts;

/// <summary>
/// Сервис построения фракционных агрегатов голосований (04-functionality.md, §1)
/// </summary>
public interface IVoteFactionStatsService
{
    /// <summary>
    /// Строит строки статистики по фракциям для одного голосования из индивидуальных голосов депутатов.
    /// Депутаты без периода фракции (<see cref="DeputyVote.FactionPeriodId"/> = null) в статистику не попадают;
    /// пустое голосование не даёт строк. Проценты считаются от состава фракции и от голосовавших,
    /// округляются до <see cref="ValidationConstants.CommonValidationConstants.PercentScale"/> знаков
    /// (половина — от нуля); при нулевом знаменателе процент равен null.
    /// </summary>
    /// <param name="voteId">Идентификатор голосования</param>
    /// <param name="deputyVotes">Индивидуальные голоса депутатов этого голосования</param>
    /// <param name="factionIdByPeriodId">Сопоставление идентификаторов периодов фракций и канонических фракций</param>
    IReadOnlyCollection<VoteFactionStats> BuildStats(
        Guid voteId,
        IReadOnlyCollection<DeputyVote> deputyVotes,
        IReadOnlyDictionary<Guid, Guid> factionIdByPeriodId);
}