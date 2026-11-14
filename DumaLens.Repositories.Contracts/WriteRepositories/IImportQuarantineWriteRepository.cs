using DumaLens.Context.Contracts;
using DumaLens.Entities;

namespace DumaLens.Repositories.Contracts.WriteRepositories;

/// <summary>
/// Репозиторий записи сущности <see cref="ImportQuarantine"/>
/// </summary>
public interface IImportQuarantineWriteRepository : IDbWriter<ImportQuarantine>
{
}