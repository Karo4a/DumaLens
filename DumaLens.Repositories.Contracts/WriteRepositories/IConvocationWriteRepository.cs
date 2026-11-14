using DumaLens.Context.Contracts;
using DumaLens.Entities;

namespace DumaLens.Repositories.Contracts.WriteRepositories;

/// <summary>
/// Репозиторий записи сущности <see cref="Convocation"/>
/// </summary>
public interface IConvocationWriteRepository : IDbWriter<Convocation>
{
}