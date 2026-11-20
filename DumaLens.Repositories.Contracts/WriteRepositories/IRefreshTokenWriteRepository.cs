using DumaLens.Context.Contracts;
using DumaLens.Entities;

namespace DumaLens.Repositories.Contracts.WriteRepositories;

/// <summary>
/// Репозиторий записи сущности <see cref="RefreshToken"/>
/// </summary>
public interface IRefreshTokenWriteRepository : IDbWriter<RefreshToken>
{
    /// <summary>
    /// Удаляет истёкшие и отозванные токены старше контрольной даты пакетами; активные не затрагиваются
    /// </summary>
    /// <param name="cutoff">Контрольная дата: записи с ExpiresAt или RevokedAt раньше неё удаляются</param>
    /// <param name="batchSize">Размер пакета удаления</param>
    /// <param name="cancellationToken">Токен отмены</param>
    Task<int> DeleteOldTokensAsync(DateTimeOffset cutoff, int batchSize, CancellationToken cancellationToken);
}