using DumaLens.Context.Contracts;
using DumaLens.Entities;
using DumaLens.Repositories.Contracts.Models;
using DumaLens.Repositories.Contracts.ReadRepositories;
using Microsoft.EntityFrameworkCore;

namespace DumaLens.Repositories.ReadRepositories;

/// <inheritdoc cref="IFactionReadRepository" />
public class FactionReadRepository : IFactionReadRepository, IRepositoryAnchor
{
    private readonly IReader reader;

    /// <summary>
    /// Конструктор
    /// </summary>
    public FactionReadRepository(IReader reader)
    {
        this.reader = reader;
    }

    Task<FactionDbModel?> IFactionReadRepository.GetById(Guid id, CancellationToken cancellationToken)
        => reader.Read<Faction>()
            .NotDeletedAt()
            .ById(id)
            .Select(x => new FactionDbModel
            {
                Id = x.Id,
                Code = x.Code,
                Title = x.Title,
            })
            .FirstOrDefaultAsync(cancellationToken);

    Task<FactionDbModel?> IFactionReadRepository.GetByCode(string code, CancellationToken cancellationToken)
        => reader.Read<Faction>()
            .NotDeletedAt()
            .Where(x => x.Code == code)
            .Select(x => new FactionDbModel
            {
                Id = x.Id,
                Code = x.Code,
                Title = x.Title,
            })
            .FirstOrDefaultAsync(cancellationToken);

    Task<IReadOnlyCollection<FactionDbModel>> IFactionReadRepository.GetAll(CancellationToken cancellationToken)
        => reader.Read<Faction>()
            .NotDeletedAt()
            .Select(x => new FactionDbModel
            {
                Id = x.Id,
                Code = x.Code,
                Title = x.Title,
            })
            .OrderBy(x => x.Code)
            .ToReadOnlyCollectionAsync(cancellationToken);

    Task<IReadOnlyCollection<FactionPeriodDbModel>> IFactionReadRepository.GetPeriodsByFaction(Guid factionId, CancellationToken cancellationToken)
        => reader.Read<FactionPeriod>()
            .NotDeletedAt()
            .Where(x => x.FactionId == factionId)
            .Select(x => new FactionPeriodDbModel
            {
                Id = x.Id,
                FactionId = x.FactionId,
                ConvocationId = x.ConvocationId,
                ConvocationNumber = x.Convocation.Number,
                SiteId = x.SiteId,
                Title = x.Title,
                DateFrom = x.DateFrom,
                DateTo = x.DateTo,
            })
            .OrderBy(x => x.DateFrom)
            .ToReadOnlyCollectionAsync(cancellationToken);

    Task<FactionPeriodDbModel?> IFactionReadRepository.GetPeriodBySiteId(Guid convocationId, long siteId, CancellationToken cancellationToken)
        => reader.Read<FactionPeriod>()
            .NotDeletedAt()
            .Where(x => x.ConvocationId == convocationId && x.SiteId == siteId)
            .Select(x => new FactionPeriodDbModel
            {
                Id = x.Id,
                FactionId = x.FactionId,
                ConvocationId = x.ConvocationId,
                ConvocationNumber = x.Convocation.Number,
                SiteId = x.SiteId,
                Title = x.Title,
                DateFrom = x.DateFrom,
                DateTo = x.DateTo,
            })
            .FirstOrDefaultAsync(cancellationToken);

    Task<IReadOnlyCollection<FactionPeriodDbModel>> IFactionReadRepository.GetAllPeriods(CancellationToken cancellationToken)
        => reader.Read<FactionPeriod>()
            .NotDeletedAt()
            .Select(x => new FactionPeriodDbModel
            {
                Id = x.Id,
                FactionId = x.FactionId,
                ConvocationId = x.ConvocationId,
                ConvocationNumber = x.Convocation.Number,
                SiteId = x.SiteId,
                Title = x.Title,
                DateFrom = x.DateFrom,
                DateTo = x.DateTo,
            })
            .OrderBy(x => x.DateFrom)
            .ToReadOnlyCollectionAsync(cancellationToken);

    Task<IReadOnlyCollection<FactionConvocationStatsDbModel>> IFactionReadRepository.GetConvocationStats(Guid factionId, CancellationToken cancellationToken)
        => reader.Read<Vote>()
            .NotDeletedAt()
            .Join(reader.Read<VoteFactionStats>(),
                vote => vote.Id,
                stat => stat.VoteId,
                (vote, stat) => new { Stat = stat })
            .Where(x => x.Stat.FactionId == factionId)
            .GroupBy(x => x.Stat.FactionPeriod.Convocation.Number)
            .Select(g => new FactionConvocationStatsDbModel
            {
                ConvocationNumber = g.Key,
                VoteCount = g.Count(),
                AvgAcceptPctMembers = g.Average(x => x.Stat.AcceptPctMembers),
                AvgDeclinePctMembers = g.Average(x => x.Stat.DeclinePctMembers),
                AvgAbstainPctMembers = g.Average(x => x.Stat.AbstainPctMembers),
                AvgNonePctMembers = g.Average(x => x.Stat.NonePctMembers),
            })
            .OrderBy(x => x.ConvocationNumber)
            .ToReadOnlyCollectionAsync(cancellationToken);
}