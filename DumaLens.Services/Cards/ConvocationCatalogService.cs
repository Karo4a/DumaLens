using AutoMapper;
using DumaLens.Repositories.Contracts.ReadRepositories;
using DumaLens.Services.Contracts;
using DumaLens.Services.Contracts.Models;

namespace DumaLens.Services.Cards;

/// <summary>
/// Сервис каталога созывов
/// </summary>
public class ConvocationCatalogService : IConvocationCatalogService, IServiceAnchor
{
    private readonly IConvocationReadRepository convocationReadRepository;
    private readonly IMapper mapper;

    /// <summary>
    /// Конструктор
    /// </summary>
    public ConvocationCatalogService(IConvocationReadRepository convocationReadRepository, IMapper mapper)
    {
        this.convocationReadRepository = convocationReadRepository;
        this.mapper = mapper;
    }

    async Task<IReadOnlyCollection<ConvocationCatalogItemModel>> IConvocationCatalogService.GetAll(CancellationToken cancellationToken)
    {
        var convocations = await convocationReadRepository.GetCatalog(cancellationToken);
        return mapper.Map<IReadOnlyCollection<ConvocationCatalogItemModel>>(convocations);
    }
}