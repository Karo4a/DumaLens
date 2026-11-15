using AutoMapper;
using DumaLens.Entities.ValidationConstants;
using DumaLens.Repositories.Contracts.ReadRepositories;
using DumaLens.Services.Contracts;
using DumaLens.Services.Contracts.Exceptions;
using DumaLens.Services.Contracts.Models;

namespace DumaLens.Services.Cards;

/// <summary>
/// Сервис карточек и поиска законопроектов
/// </summary>
public class BillCardService : IBillCardService, IServiceAnchor
{
    private readonly IValidateService validateService;
    private readonly IBillReadRepository billReadRepository;
    private readonly IMapper mapper;

    /// <summary>
    /// Конструктор
    /// </summary>
    public BillCardService(IValidateService validateService, IBillReadRepository billReadRepository, IMapper mapper)
    {
        this.validateService = validateService;
        this.billReadRepository = billReadRepository;
        this.mapper = mapper;
    }

    async Task<BillCardModel> IBillCardService.GetCard(string number, CancellationToken cancellationToken)
    {
        var bill = await billReadRepository.GetDetailsByNumber(number, cancellationToken);
        if (bill == null)
        {
            throw new DumaLensNotFoundException($"Законопроект «{number}» не найден");
        }
        return mapper.Map<BillCardModel>(bill);
    }

    async Task<IReadOnlyCollection<BillSearchItemModel>> IBillCardService.Search(BillSearchQueryModel query, CancellationToken cancellationToken)
    {
        await validateService.Validate(query, cancellationToken);
        var bills = await billReadRepository.SearchByNumberPart(query.NumberPart.Trim(), BillValidationConstants.SearchResultLimit, cancellationToken);
        return mapper.Map<IReadOnlyCollection<BillSearchItemModel>>(bills);
    }
}