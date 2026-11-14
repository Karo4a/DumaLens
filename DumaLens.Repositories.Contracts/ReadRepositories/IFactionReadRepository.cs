using DumaLens.Entities;
using DumaLens.Repositories.Contracts.Models;

namespace DumaLens.Repositories.Contracts.ReadRepositories;

/// <summary>
/// Репозиторий чтения сущностей <see cref="Faction"/> и <see cref="FactionPeriod"/>
/// </summary>
public interface IFactionReadRepository
{
    /// <summary>
    /// Получает <see cref="Faction"/> по идентификатору
    /// </summary>
    Task<FactionDbModel?> GetById(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Получает <see cref="Faction"/> по коду
    /// </summary>
    Task<FactionDbModel?> GetByCode(string code, CancellationToken cancellationToken);

    /// <summary>
    /// Получает коллекцию <see cref="Faction"/>, упорядоченную по коду
    /// </summary>
    Task<IReadOnlyCollection<FactionDbModel>> GetAll(CancellationToken cancellationToken);

    /// <summary>
    /// Получает периоды фракции, упорядоченные по дате начала
    /// </summary>
    Task<IReadOnlyCollection<FactionPeriodDbModel>> GetPeriodsByFaction(Guid factionId, CancellationToken cancellationToken);

    /// <summary>
    /// Получает период фракции по идентификатору сайта в рамках созыва
    /// </summary>
    Task<FactionPeriodDbModel?> GetPeriodBySiteId(Guid convocationId, long siteId, CancellationToken cancellationToken);
}