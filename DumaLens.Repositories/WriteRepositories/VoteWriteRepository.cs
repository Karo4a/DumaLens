using DumaLens.Common.Contracts;
using DumaLens.Context.Contracts;
using DumaLens.Entities;
using DumaLens.Repositories.Contracts.WriteRepositories;

namespace DumaLens.Repositories.WriteRepositories;

/// <inheritdoc cref="IVoteWriteRepository" />
public class VoteWriteRepository : BaseWriteRepository<Vote>, IVoteWriteRepository, IRepositoryAnchor
{
    /// <summary>
    /// Конструктор
    /// </summary>
    public VoteWriteRepository(IWriter writer, IDateTimeProvider dateTimeProvider)
        : base(writer, dateTimeProvider)
    {
    }
}