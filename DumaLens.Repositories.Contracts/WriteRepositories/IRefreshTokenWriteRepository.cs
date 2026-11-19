using DumaLens.Context.Contracts;
using DumaLens.Entities;

namespace DumaLens.Repositories.Contracts.WriteRepositories;

/// <summary>
/// Репозиторий записи сущности <see cref="RefreshToken"/>
/// </summary>
public interface IRefreshTokenWriteRepository : IDbWriter<RefreshToken>
{
}