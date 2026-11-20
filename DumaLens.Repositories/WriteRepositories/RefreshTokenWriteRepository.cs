using DumaLens.Common.Contracts;
using DumaLens.Context.Contracts;
using DumaLens.Entities;
using DumaLens.Repositories.Contracts.WriteRepositories;
using Microsoft.EntityFrameworkCore;

namespace DumaLens.Repositories.WriteRepositories;

/// <inheritdoc cref="IRefreshTokenWriteRepository" />
public class RefreshTokenWriteRepository : BaseWriteRepository<RefreshToken>, IRefreshTokenWriteRepository, IRepositoryAnchor
{
    private readonly IReader reader;

    /// <summary>
    /// Конструктор
    /// </summary>
    public RefreshTokenWriteRepository(IWriter writer, IReader reader, IDateTimeProvider dateTimeProvider)
        : base(writer, dateTimeProvider)
    {
        this.reader = reader;
    }

    async Task<int> IRefreshTokenWriteRepository.DeleteOldTokensAsync(DateTimeOffset cutoff, int batchSize, CancellationToken cancellationToken)
    {
        var totalDeleted = 0;
        while (true)
        {
            var ids = await reader.Read<RefreshToken>()
                .Where(x => x.ExpiresAt < cutoff || x.RevokedAt < cutoff)
                .OrderBy(x => x.Id)
                .Select(x => x.Id)
                .Take(batchSize)
                .ToListAsync(cancellationToken);
            if (ids.Count == 0)
            {
                break;
            }
            totalDeleted += await reader.Read<RefreshToken>()
                .Where(x => ids.Contains(x.Id))
                .ExecuteDeleteAsync(cancellationToken);
        }
        return totalDeleted;
    }
}