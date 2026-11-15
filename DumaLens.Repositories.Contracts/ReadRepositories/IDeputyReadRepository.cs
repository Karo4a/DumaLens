using DumaLens.Repositories.Contracts.Models;

namespace DumaLens.Repositories.Contracts.ReadRepositories;

/// <summary>
/// Репозиторий чтения депутатов
/// </summary>
public interface IDeputyReadRepository
{
    /// <summary>
    /// Получает карточку депутата по идентификатору сайта (null — не найден)
    /// </summary>
    Task<DeputyCardDbModel?> GetCardBySiteId(long siteId, CancellationToken cancellationToken);

    /// <summary>
    /// Получает депутатов, состоявших во фракциях созыва
    /// </summary>
    Task<IReadOnlyCollection<DeputyListItemDbModel>> GetByConvocation(Guid convocationId, CancellationToken cancellationToken);
}