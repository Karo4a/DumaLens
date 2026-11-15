using DumaLens.Context.Contracts;
using DumaLens.Entities;
using DumaLens.Repositories.Contracts.Models;
using DumaLens.Repositories.Contracts.ReadRepositories;
using Microsoft.EntityFrameworkCore;

namespace DumaLens.Repositories.ReadRepositories;

/// <summary>
/// Репозиторий чтения сводных показателей базы
/// </summary>
public class StatsReadRepository : IStatsReadRepository, IRepositoryAnchor
{
    private readonly IReader reader;

    /// <summary>
    /// Конструктор
    /// </summary>
    public StatsReadRepository(IReader reader)
    {
        this.reader = reader;
    }

    async Task<StatsOverviewDbModel> IStatsReadRepository.GetOverview(CancellationToken cancellationToken)
    {
        var votes = reader.Read<Vote>().NotDeletedAt();
        // Диапазон дат — в московском времени каждого голосования (историческое смещение)
        var dateFrom = await votes
            .Select(x => (DateTimeOffset?)x.VotedAt.AddMinutes(x.UtcOffsetMinutes))
            .MinAsync(cancellationToken);
        var dateTo = await votes
            .Select(x => (DateTimeOffset?)x.VotedAt.AddMinutes(x.UtcOffsetMinutes))
            .MaxAsync(cancellationToken);
        return new StatsOverviewDbModel
        {
            VoteCount = await votes.CountAsync(cancellationToken),
            DeputyCount = await reader.Read<Deputy>().NotDeletedAt().CountAsync(cancellationToken),
            FactionCount = await reader.Read<Faction>().NotDeletedAt().CountAsync(cancellationToken),
            BillCount = await reader.Read<Bill>().NotDeletedAt().CountAsync(cancellationToken),
            DateFrom = dateFrom,
            DateTo = dateTo,
        };
    }
}