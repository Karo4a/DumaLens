using DumaLens.Services.Contracts.Models;

namespace DumaLens.Services.Contracts;

/// <summary>
/// Сервис карточек и списков депутатов
/// </summary>
public interface IDeputyCardService
{
    /// <summary>
    /// Получает карточку депутата по идентификатору сайта
    /// </summary>
    /// <exception cref="Exceptions.DumaLensNotFoundException">Депутат не найден</exception>
    Task<DeputyCardModel> GetCard(long siteId, CancellationToken cancellationToken);

    /// <summary>
    /// Получает список депутатов созыва
    /// </summary>
    Task<IReadOnlyCollection<DeputyListItemModel>> GetByConvocation(DeputyListQueryModel query, CancellationToken cancellationToken);
}