using DumaLens.Services.Contracts.Models;

namespace DumaLens.Services.Contracts;

/// <summary>
/// Сервис карточек голосований
/// </summary>
public interface IVoteCardService
{
    /// <summary>
    /// Получает полную карточку голосования по идентификатору сайта
    /// </summary>
    /// <exception cref="Exceptions.DumaLensNotFoundException">Голосование не найдено</exception>
    Task<VoteDetailsModel> GetDetails(long siteId, CancellationToken cancellationToken);
}