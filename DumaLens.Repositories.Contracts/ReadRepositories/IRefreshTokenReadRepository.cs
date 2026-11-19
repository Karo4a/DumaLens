using DumaLens.Entities;
using DumaLens.Repositories.Contracts.Models;

namespace DumaLens.Repositories.Contracts.ReadRepositories;

/// <summary>
/// Репозиторий чтения сущности <see cref="RefreshToken"/>
/// </summary>
public interface IRefreshTokenReadRepository
{
    /// <summary>
    /// Получает <see cref="RefreshToken"/> по хэшу токена (включая отозванные — для обнаружения повторного использования)
    /// </summary>
    Task<RefreshTokenDbModel?> GetByTokenHash(string tokenHash, CancellationToken cancellationToken);

    /// <summary>
    /// Получает активный (не отозванный) <see cref="RefreshToken"/> пользователя
    /// </summary>
    Task<RefreshTokenDbModel?> GetActiveByUserId(Guid userId, CancellationToken cancellationToken);
}