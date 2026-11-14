using DumaLens.Context.Contracts;
using DumaLens.Entities;

namespace DumaLens.Repositories.Contracts.WriteRepositories;

/// <summary>
/// Репозиторий записи сущности <see cref="FactionPeriod"/>
/// </summary>
public interface IFactionPeriodWriteRepository : IDbWriter<FactionPeriod>
{
}