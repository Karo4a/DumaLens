using DumaLens.Context.Contracts;
using DumaLens.Entities;
using DumaLens.Repositories.Contracts.Models;
using DumaLens.Repositories.Contracts.ReadRepositories;
using Microsoft.EntityFrameworkCore;

namespace DumaLens.Repositories.ReadRepositories;

/// <inheritdoc cref="IBillReadRepository"/>
public class BillReadRepository : IBillReadRepository, IRepositoryAnchor
{
    private readonly IReader reader;

    /// <summary>
    /// Конструктор
    /// </summary>
    public BillReadRepository(IReader reader)
    {
        this.reader = reader;
    }

    Task<BillDbModel?> IBillReadRepository.GetByNumber(string number, CancellationToken cancellationToken)
        => reader.Read<Bill>()
            .NotDeletedAt()
            .Where(x => x.Number == number)
            .Select(x => new BillDbModel
            {
                Id = x.Id,
                Number = x.Number,
            })
            .FirstOrDefaultAsync(cancellationToken);

    async Task<BillDetailsDbModel?> IBillReadRepository.GetDetailsByNumber(string number, CancellationToken cancellationToken)
    {
        var bill = await reader.Read<Bill>()
            .NotDeletedAt()
            .Where(x => x.Number == number)
            .Select(x => new BillDetailsDbModel
            {
                Id = x.Id,
                Number = x.Number,
            })
            .FirstOrDefaultAsync(cancellationToken);
        if (bill == null)
        {
            return null;
        }
        var votes = await reader.Read<Vote>()
            .NotDeletedAt()
            .Where(x => x.BillId == bill.Id)
            .OrderBy(x => x.VotedAt)
            .ThenBy(x => x.Id)
            .Select(x => new BillVoteDbModel
            {
                Id = x.Id,
                SiteId = x.SiteId,
                VotedAt = x.VotedAt,
                UtcOffsetMinutes = x.UtcOffsetMinutes,
                Stage = x.Stage,
                Outcome = x.Outcome,
            })
            .ToListAsync(cancellationToken);
        await AttachFactionPositions(votes, cancellationToken);
        bill.Votes = votes;
        return bill;
    }

    Task<IReadOnlyCollection<BillDbModel>> IBillReadRepository.SearchByNumberPart(string numberPart, int limit, CancellationToken cancellationToken)
        => reader.Read<Bill>()
            .NotDeletedAt()
            .Where(x => x.Number.Contains(numberPart))
            .OrderBy(x => x.Number)
            .Take(limit)
            .Select(x => new BillDbModel
            {
                Id = x.Id,
                Number = x.Number,
            })
            .ToReadOnlyCollectionAsync(cancellationToken);

    private async Task AttachFactionPositions(List<BillVoteDbModel> votes, CancellationToken cancellationToken)
    {
        if (votes.Count == 0)
        {
            return;
        }
        var voteIds = votes.Select(x => x.Id).ToList();
        var positions = await reader.Read<VoteFactionStats>()
            .Where(x => voteIds.Contains(x.VoteId))
            .Select(x => new BillFactionPositionDbModel
            {
                VoteId = x.VoteId,
                FactionCode = x.Faction.Code,
                FactionTitle = x.Faction.Title,
                AcceptPctMembers = x.AcceptPctMembers,
                DeclinePctMembers = x.DeclinePctMembers,
                AbstainPctMembers = x.AbstainPctMembers,
                NonePctMembers = x.NonePctMembers,
            })
            .OrderBy(x => x.FactionCode)
            .ThenBy(x => x.VoteId)
            .ToListAsync(cancellationToken);
        var positionsByVote = positions
            .GroupBy(x => x.VoteId)
            .ToDictionary(x => x.Key, x => (IReadOnlyCollection<BillFactionPositionDbModel>)x.ToList());
        foreach (var vote in votes)
        {
            vote.FactionPositions = positionsByVote.TryGetValue(vote.Id, out var rows) ? rows : [];
        }
    }
}