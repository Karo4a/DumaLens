using DumaLens.Common.Contracts;
using DumaLens.Context.Contracts;
using DumaLens.Entities;
using DumaLens.Repositories.Contracts.WriteRepositories;

namespace DumaLens.Repositories.WriteRepositories;

/// <inheritdoc cref="IDeputyFactionMembershipWriteRepository" />
public class DeputyFactionMembershipWriteRepository : BaseWriteRepository<DeputyFactionMembership>, IDeputyFactionMembershipWriteRepository, IRepositoryAnchor
{
    /// <summary>
    /// Конструктор
    /// </summary>
    public DeputyFactionMembershipWriteRepository(IWriter writer, IDateTimeProvider dateTimeProvider)
        : base(writer, dateTimeProvider)
    {
    }
}