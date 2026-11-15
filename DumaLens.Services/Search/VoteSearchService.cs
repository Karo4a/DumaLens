using AutoMapper;
using DumaLens.Repositories.Contracts.Enums;
using DumaLens.Repositories.Contracts.Models;
using DumaLens.Repositories.Contracts.ReadRepositories;
using DumaLens.Services.Contracts;
using DumaLens.Services.Contracts.Exceptions;
using DumaLens.Services.Contracts.Models;

namespace DumaLens.Services.Search;

/// <inheritdoc cref="IVoteSearchService"/>
public class VoteSearchService : IVoteSearchService, IServiceAnchor
{
    private readonly IValidateService validateService;
    private readonly IVoteReadRepository voteReadRepository;
    private readonly IConvocationReadRepository convocationReadRepository;
    private readonly IBillReadRepository billReadRepository;
    private readonly IFactionReadRepository factionReadRepository;
    private readonly IMapper mapper;

    /// <summary>
    /// Конструктор
    /// </summary>
    public VoteSearchService(
        IValidateService validateService,
        IVoteReadRepository voteReadRepository,
        IConvocationReadRepository convocationReadRepository,
        IBillReadRepository billReadRepository,
        IFactionReadRepository factionReadRepository,
        IMapper mapper)
    {
        this.validateService = validateService;
        this.voteReadRepository = voteReadRepository;
        this.convocationReadRepository = convocationReadRepository;
        this.billReadRepository = billReadRepository;
        this.factionReadRepository = factionReadRepository;
        this.mapper = mapper;
    }

    async Task<VoteSearchResultModel> IVoteSearchService.Search(VoteSearchQueryModel query, CancellationToken cancellationToken)
    {
        await validateService.Validate(query, cancellationToken);
        Guid? convocationId = null;
        if (query.Convocation is { } convocationNumber)
        {
            var convocation = await convocationReadRepository.GetByNumber(convocationNumber, cancellationToken);
            if (convocation == null)
            {
                return EmptyPage(query);
            }
            convocationId = convocation.Id;
        }
        Guid? billId = null;
        if (!string.IsNullOrWhiteSpace(query.BillNumber))
        {
            var bill = await billReadRepository.GetByNumber(query.BillNumber.Trim(), cancellationToken);
            if (bill == null)
            {
                return EmptyPage(query);
            }
            billId = bill.Id;
        }
        var conditions = await ResolveFactionConditions(query.FactionConditions, cancellationToken);
        var spec = new VoteSearchSpec
        {
            Query = string.IsNullOrWhiteSpace(query.Query) ? null : query.Query.Trim(),
            ConvocationId = convocationId,
            DateFrom = query.DateFrom,
            DateTo = query.DateTo,
            BillId = billId,
            Stage = SearchTokenParser.ParseStage(query.Stage),
            Outcome = SearchTokenParser.ParseOutcome(query.Outcome),
            HasResults = query.HasResults ?? (conditions.Count > 0 ? true : null),
            FactionConditions = conditions,
            Sort = SearchTokenParser.ParseSort(query.Sort) ?? VoteSearchSort.DateDesc,
            Page = query.Page,
            PageSize = query.PageSize,
        };
        var page = await voteReadRepository.Search(spec, cancellationToken);
        return mapper.Map<VoteSearchResultModel>(page);
    }

    /// <summary>
    /// Разрешает фракционные условия до идентификаторов канонических фракций
    /// </summary>
    /// <exception cref="DumaLensValidationException">Неизвестная фракция (код, название или историческое название)</exception>
    private async Task<IReadOnlyCollection<VoteFactionConditionSpec>> ResolveFactionConditions(IReadOnlyCollection<FactionConditionQueryModel> conditions, CancellationToken cancellationToken)
    {
        if (conditions.Count == 0)
        {
            return [];
        }
        var factionIndex = await BuildFactionIndex(cancellationToken);
        var resolved = new List<VoteFactionConditionSpec>(conditions.Count);
        foreach (var condition in conditions)
        {
            var token = condition.Faction.Trim();
            if (!factionIndex.TryGetValue(token, out var factionId))
            {
                throw new DumaLensValidationException([InvalidateItemModel.New("Faction", $"Неизвестная фракция: «{token}»")]);
            }
            resolved.Add(new VoteFactionConditionSpec
            {
                FactionId = factionId,
                // Токены гарантированно распознаны валидатором
                Result = SearchTokenParser.ParseResult(condition.Result)!.Value,
                Operator = SearchTokenParser.ParseOperator(condition.Operator)!.Value,
                Threshold = condition.Threshold,
                Basis = SearchTokenParser.ParseBasis(condition.Basis)!.Value,
            });
        }
        return resolved;
    }

    /// <summary>
    /// Строит индекс «токен фракции → канонический идентификатор»: коды, канонические и исторические названия
    /// </summary>
    /// <remarks>
    /// Исторические названия периодов («СР-Р», «Е-ЕР», «НЛ») резолвятся в канон фракции;
    /// при совпадении токена канонический код/название имеет приоритет над историческим.
    /// Сравнение регистронезависимое.
    /// </remarks>
    private async Task<Dictionary<string, Guid>> BuildFactionIndex(CancellationToken cancellationToken)
    {
        var factions = await factionReadRepository.GetAll(cancellationToken);
        var periods = await factionReadRepository.GetAllPeriods(cancellationToken);
        var index = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        foreach (var period in periods)
        {
            index.TryAdd(period.Title, period.FactionId);
        }
        foreach (var faction in factions)
        {
            index[faction.Title] = faction.Id;
            index[faction.Code] = faction.Id;
        }
        return index;
    }

    /// <summary>
    /// Пустая выдача для фильтра без совпадений (неизвестный номер законопроекта или созыва)
    /// </summary>
    private static VoteSearchResultModel EmptyPage(VoteSearchQueryModel query)
        => new VoteSearchResultModel
        {
            TotalCount = 0,
            Page = query.Page,
            PageSize = query.PageSize,
            Items = [],
        };
}