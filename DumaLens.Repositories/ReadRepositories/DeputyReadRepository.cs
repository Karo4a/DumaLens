using DumaLens.Context.Contracts;
using DumaLens.Entities;
using DumaLens.Entities.Contracts.Enums;
using DumaLens.Repositories.Contracts.Models;
using DumaLens.Repositories.Contracts.ReadRepositories;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;

namespace DumaLens.Repositories.ReadRepositories;

/// <summary>
/// Репозиторий чтения депутатов
/// </summary>
public class DeputyReadRepository : IDeputyReadRepository, IRepositoryAnchor
{
    private readonly IReader reader;

    /// <summary>
    /// Конструктор
    /// </summary>
    public DeputyReadRepository(IReader reader)
    {
        this.reader = reader;
    }

    async Task<DeputyCardDbModel?> IDeputyReadRepository.GetCardBySiteId(long siteId, CancellationToken cancellationToken)
    {
        var deputy = await reader.Read<Deputy>()
            .NotDeletedAt()
            .Where(x => x.SiteId == siteId)
            .Select(x => new DeputyCardDbModel
            {
                Id = x.Id,
                SiteId = x.SiteId,
                FullName = x.FullName,
                SortName = x.SortName,
            })
            .FirstOrDefaultAsync(cancellationToken);
        if (deputy == null)
        {
            return null;
        }
        deputy.Memberships = await reader.Read<DeputyFactionMembership>()
            .NotDeletedAt()
            .Where(x => x.DeputyId == deputy.Id)
            .Select(x => new DeputyMembershipDbModel
            {
                ConvocationNumber = x.FactionPeriod.Convocation.Number,
                FactionCode = x.FactionPeriod.Faction.Code,
                FactionTitle = x.FactionPeriod.Faction.Title,
                PeriodTitle = x.FactionPeriod.Title,
                DateFrom = x.DateFrom,
                DateTo = x.DateTo,
            })
            .OrderBy(x => x.ConvocationNumber)
            .ThenBy(x => x.DateFrom)
            .ToReadOnlyCollectionAsync(cancellationToken);
        // Счётчики личных результатов — только по неудалённым голосованиям
        var counters = await reader.Read<Vote>()
            .NotDeletedAt()
            .Join(reader.Read<DeputyVote>(),
                vote => vote.Id,
                deputyVote => deputyVote.VoteId,
                (vote, deputyVote) => deputyVote)
            .Where(x => x.DeputyId == deputy.Id)
            .GroupBy(x => x.Result)
            .Select(g => new { Result = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);
        var countByResult = counters.ToDictionary(x => x.Result, x => x.Count);
        deputy.AcceptCount = countByResult.GetValueOrDefault(VoteResult.Accept);
        deputy.DeclineCount = countByResult.GetValueOrDefault(VoteResult.Decline);
        deputy.AbstainCount = countByResult.GetValueOrDefault(VoteResult.Abstain);
        deputy.NoneCount = countByResult.GetValueOrDefault(VoteResult.None);
        return deputy;
    }

    async Task<IReadOnlyCollection<DeputyListItemDbModel>> IDeputyReadRepository.GetByConvocation(Guid convocationId, CancellationToken cancellationToken)
    {
        var memberships = reader.Read<DeputyFactionMembership>()
            .NotDeletedAt()
            .Where(x => x.FactionPeriod.ConvocationId == convocationId);
        var rows = await reader.Read<Deputy>()
            .NotDeletedAt()
            .Join(memberships,
                deputy => deputy.Id,
                membership => membership.DeputyId,
                (deputy, membership) => new
                {
                    deputy.SiteId,
                    deputy.FullName,
                    deputy.SortName,
                    FactionCode = membership.FactionPeriod.Faction.Code,
                })
            .ToListAsync(cancellationToken);
        var items = rows
            .GroupBy(x => x.SiteId)
            .Select(g => new DeputyListItemDbModel
            {
                SiteId = g.Key,
                FullName = g.First().FullName,
                SortName = g.First().SortName,
                FactionCodes = g.Select(x => x.FactionCode).Distinct().OrderBy(x => x).ToList(),
            })
            .OrderBy(x => x.SortName)
            .ThenBy(x => x.SiteId)
            .ToList();
        return new ReadOnlyCollection<DeputyListItemDbModel>(items);
    }
}