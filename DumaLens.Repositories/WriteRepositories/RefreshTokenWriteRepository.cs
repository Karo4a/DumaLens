using DumaLens.Common.Contracts;
using DumaLens.Context.Contracts;
using DumaLens.Entities;
using DumaLens.Repositories.Contracts.WriteRepositories;

namespace DumaLens.Repositories.WriteRepositories;

/// <inheritdoc cref="IRefreshTokenWriteRepository" />
public class RefreshTokenWriteRepository : BaseWriteRepository<RefreshToken>, IRefreshTokenWriteRepository, IRepositoryAnchor
{
    /// <summary>
    /// Конструктор
    /// </summary>
    public RefreshTokenWriteRepository(IWriter writer, IDateTimeProvider dateTimeProvider)
        : base(writer, dateTimeProvider)
    {
    }
}