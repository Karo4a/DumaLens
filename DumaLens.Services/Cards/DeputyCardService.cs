using AutoMapper;
using DumaLens.Entities.ValidationConstants;
using DumaLens.Repositories.Contracts.ReadRepositories;
using DumaLens.Services.Contracts;
using DumaLens.Services.Contracts.Exceptions;
using DumaLens.Services.Contracts.Models;

namespace DumaLens.Services.Cards;

/// <summary>
/// Сервис карточек и списков депутатов
/// </summary>
public class DeputyCardService : IDeputyCardService, IServiceAnchor
{
    private readonly IValidateService validateService;
    private readonly IDeputyReadRepository deputyReadRepository;
    private readonly IConvocationReadRepository convocationReadRepository;
    private readonly IMapper mapper;

    /// <summary>
    /// Конструктор
    /// </summary>
    public DeputyCardService(
        IValidateService validateService,
        IDeputyReadRepository deputyReadRepository,
        IConvocationReadRepository convocationReadRepository,
        IMapper mapper)
    {
        this.validateService = validateService;
        this.deputyReadRepository = deputyReadRepository;
        this.convocationReadRepository = convocationReadRepository;
        this.mapper = mapper;
    }

    async Task<DeputyCardModel> IDeputyCardService.GetCard(long siteId, CancellationToken cancellationToken)
    {
        var deputy = await deputyReadRepository.GetCardBySiteId(siteId, cancellationToken);
        if (deputy == null)
        {
            throw new DumaLensNotFoundException($"Депутат с идентификатором {siteId} не найден");
        }
        var model = mapper.Map<DeputyCardModel>(deputy);
        model.TotalVotes = model.AcceptCount + model.DeclineCount + model.AbstainCount + model.NoneCount;
        var participated = model.AcceptCount + model.DeclineCount + model.AbstainCount;
        model.ParticipationPct = model.TotalVotes == 0
            ? null
            : Math.Round(participated * 100m / model.TotalVotes, CommonValidationConstants.PercentScale, MidpointRounding.AwayFromZero);
        return model;
    }

    async Task<IReadOnlyCollection<DeputyListItemModel>> IDeputyCardService.GetByConvocation(DeputyListQueryModel query, CancellationToken cancellationToken)
    {
        await validateService.Validate(query, cancellationToken);
        var convocation = await convocationReadRepository.GetByNumber(query.Convocation, cancellationToken);
        if (convocation == null)
        {
            return [];
        }
        var deputies = await deputyReadRepository.GetByConvocation(convocation.Id, cancellationToken);
        return mapper.Map<IReadOnlyCollection<DeputyListItemModel>>(deputies);
    }
}