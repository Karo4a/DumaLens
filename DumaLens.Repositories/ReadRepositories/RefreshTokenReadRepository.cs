using DumaLens.Context.Contracts;
using DumaLens.Entities;
using DumaLens.Repositories.Contracts.Models;
using DumaLens.Repositories.Contracts.ReadRepositories;
using Microsoft.EntityFrameworkCore;

namespace DumaLens.Repositories.ReadRepositories;

/// <inheritdoc cref="IRefreshTokenReadRepository" />
public class RefreshTokenReadRepository : IRefreshTokenReadRepository, IRepositoryAnchor
{
    private readonly IReader reader;

    /// <summary>
    /// Конструктор
    /// </summary>
    public RefreshTokenReadRepository(IReader reader)
    {
        this.reader = reader;
    }

    Task<RefreshTokenDbModel?> IRefreshTokenReadRepository.GetByTokenHash(string tokenHash, CancellationToken cancellationToken)
        => Project(reader.Read<RefreshToken>()
                .Where(x => x.TokenHash == tokenHash))
            .FirstOrDefaultAsync(cancellationToken);

    Task<RefreshTokenDbModel?> IRefreshTokenReadRepository.GetActiveByUserId(Guid userId, CancellationToken cancellationToken)
        => Project(reader.Read<RefreshToken>()
                .Where(x => x.UserId == userId && x.RevokedAt == null))
            .FirstOrDefaultAsync(cancellationToken);

    private static IQueryable<RefreshTokenDbModel> Project(IQueryable<RefreshToken> query)
        => query.Select(x => new RefreshTokenDbModel
        {
            Id = x.Id,
            UserId = x.UserId,
            TokenHash = x.TokenHash,
            SecurityStamp = x.SecurityStamp,
            ExpiresAt = x.ExpiresAt,
            CreatedAt = x.CreatedAt,
            RevokedAt = x.RevokedAt,
        });
}