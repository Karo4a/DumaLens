using DumaLens.Context.Contracts;
using DumaLens.Entities;

namespace DumaLens.Repositories.Contracts.WriteRepositories;

/// <summary>
/// Репозиторий записи сущности <see cref="Deputy"/>
/// </summary>
public interface IDeputyWriteRepository : IDbWriter<Deputy>
{
}