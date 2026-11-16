using DumaLens.Entities;
using DumaLens.Entities.Contracts.Enums;
using DumaLens.Entities.ValidationConstants;
using DumaLens.Services.Contracts;

namespace DumaLens.Services.Stats;

/// <inheritdoc cref="IVoteFactionStatsService"/>
public class VoteFactionStatsService : IVoteFactionStatsService, IServiceAnchor
{
    /// <inheritdoc cref="IVoteFactionStatsService.BuildStats"/>
    public IReadOnlyCollection<VoteFactionStats> BuildStats(
        Guid voteId,
        IReadOnlyCollection<DeputyVote> deputyVotes,
        IReadOnlyDictionary<short, FactionPeriodStatsKey> periodKeys)
    {
        var statsRows = new List<VoteFactionStats>();
        var groups = deputyVotes
            .Where(x => x.FactionPeriodId.HasValue)
            .GroupBy(x => x.FactionPeriodId!.Value);
        foreach (var group in groups)
        {
            var periodVotes = group.ToList();
            var acceptCount = periodVotes.Count(x => x.Result == VoteResult.Accept);
            var declineCount = periodVotes.Count(x => x.Result == VoteResult.Decline);
            var abstainCount = periodVotes.Count(x => x.Result == VoteResult.Abstain);
            var noneCount = periodVotes.Count(x => x.Result == VoteResult.None);
            var memberCount = acceptCount + declineCount + abstainCount + noneCount;
            var votedCount = acceptCount + declineCount + abstainCount;
            statsRows.Add(new VoteFactionStats
            {
                VoteId = voteId,
                FactionPeriodId = periodKeys[group.Key].PeriodId,
                FactionId = periodKeys[group.Key].FactionId,
                AcceptCount = acceptCount,
                DeclineCount = declineCount,
                AbstainCount = abstainCount,
                NoneCount = noneCount,
                MemberCount = memberCount,
                VotedCount = votedCount,
                AcceptPctMembers = Percent(acceptCount, memberCount),
                DeclinePctMembers = Percent(declineCount, memberCount),
                AbstainPctMembers = Percent(abstainCount, memberCount),
                NonePctMembers = Percent(noneCount, memberCount),
                AcceptPctVoted = Percent(acceptCount, votedCount),
                DeclinePctVoted = Percent(declineCount, votedCount),
                AbstainPctVoted = Percent(abstainCount, votedCount),
            });
        }

        return statsRows;
    }

    /// <summary>
    /// Процент count от total с округлением «половина — от нуля» (как ROUND в PostgreSQL при пересчёте);
    /// null при нулевом знаменателе
    /// </summary>
    private static decimal? Percent(int count, int total)
    {
        return total == 0
            ? null
            : Math.Round(count * 100m / total, CommonValidationConstants.PercentScale, MidpointRounding.AwayFromZero);
    }
}