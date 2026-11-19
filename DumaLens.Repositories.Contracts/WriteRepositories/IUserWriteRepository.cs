using DumaLens.Context.Contracts;
using DumaLens.Entities;

namespace DumaLens.Repositories.Contracts.WriteRepositories;

/// <summary>
/// Репозиторий записи сущности <see cref="User"/>
/// </summary>
public interface IUserWriteRepository : IDbWriter<User>
{
}