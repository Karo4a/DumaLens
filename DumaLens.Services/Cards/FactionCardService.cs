using AutoMapper;
using DumaLens.Entities.ValidationConstants;
using DumaLens.Repositories.Contracts.ReadRepositories;
using DumaLens.Services.Contracts;
using DumaLens.Services.Contracts.Exceptions;
using DumaLens.Services.Contracts.Models;

namespace DumaLens.Services.Cards;

/// <summary>
/// Сервис карточек фракций
/// </summary>
public class FactionCardService : IFactionCardService, IServiceAnchor
{
    private readonly IFactionReadRepository factionReadRepository;
    private readonly IMapper mapper;

    /// <summary>
    /// Конструктор
    /// </summary>
    public FactionCardService(IFactionReadRepository factionReadRepository, IMapper mapper)
    {
        this.factionReadRepository = factionReadRepository;
        this.mapper = mapper;
    }

    async Task<IReadOnlyCollection<FactionCardModel>> IFactionCardService.GetAll(CancellationToken cancellationToken)
    {
        var factions = await factionReadRepository.GetAll(cancellationToken);
        var periods = await factionReadRepository.GetAllPeriods(cancellationToken);
        var periodsByFaction = periods
            .GroupBy(x => x.FactionId)
            .ToDictionary(x => x.Key, x => x.ToList());
        var models = new List<FactionCardModel>(factions.Count);
        foreach (var faction in factions)
        {
            var model = mapper.Map<FactionCardModel>(faction);
            model.Periods = periodsByFaction.TryGetValue(faction.Id, out var rows)
                ? mapper.Map<IReadOnlyCollection<FactionPeriodModel>>(rows)
                : [];
            models.Add(model);
        }
        return models;
    }

    async Task<FactionCardModel> IFactionCardService.GetCard(string code, CancellationToken cancellationToken)
    {
        var faction = await factionReadRepository.GetByCode(code, cancellationToken);
        if (faction == null)
        {
            throw new DumaLensNotFoundException($"Фракция «{code}» не найдена");
        }
        var periods = await factionReadRepository.GetPeriodsByFaction(faction.Id, cancellationToken);
        var stats = await factionReadRepository.GetConvocationStats(faction.Id, cancellationToken);
        var model = mapper.Map<FactionCardModel>(faction);
        model.Periods = mapper.Map<IReadOnlyCollection<FactionPeriodModel>>(periods);
        model.ConvocationStats = RoundStats(mapper.Map<IReadOnlyCollection<FactionConvocationStatsModel>>(stats));
        return model;
    }

    private static IReadOnlyCollection<FactionConvocationStatsModel> RoundStats(IReadOnlyCollection<FactionConvocationStatsModel> stats)
    {
        foreach (var stat in stats)
        {
            stat.AvgAcceptPctMembers = Round(stat.AvgAcceptPctMembers);
            stat.AvgDeclinePctMembers = Round(stat.AvgDeclinePctMembers);
            stat.AvgAbstainPctMembers = Round(stat.AvgAbstainPctMembers);
            stat.AvgNonePctMembers = Round(stat.AvgNonePctMembers);
        }
        return stats;
    }

    private static decimal? Round(decimal? value)
        => value.HasValue ? Math.Round(value.Value, CommonValidationConstants.PercentScale, MidpointRounding.AwayFromZero) : null;
}