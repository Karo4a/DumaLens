using AutoMapper;
using DumaLens.Repositories.Contracts.ReadRepositories;
using DumaLens.Services.Contracts;
using DumaLens.Services.Contracts.Models;

namespace DumaLens.Services.Stats;

/// <summary>
/// Сервис сводных показателей базы
/// </summary>
public class StatsOverviewService : IStatsOverviewService, IServiceAnchor
{
    private readonly IStatsReadRepository statsReadRepository;
    private readonly IMapper mapper;

    /// <summary>
    /// Конструктор
    /// </summary>
    public StatsOverviewService(IStatsReadRepository statsReadRepository, IMapper mapper)
    {
        this.statsReadRepository = statsReadRepository;
        this.mapper = mapper;
    }

    async Task<StatsOverviewModel> IStatsOverviewService.GetOverview(CancellationToken cancellationToken)
    {
        var overview = await statsReadRepository.GetOverview(cancellationToken);
        return mapper.Map<StatsOverviewModel>(overview);
    }
}