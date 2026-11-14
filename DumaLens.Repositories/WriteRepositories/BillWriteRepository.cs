using DumaLens.Common.Contracts;
using DumaLens.Context.Contracts;
using DumaLens.Entities;
using DumaLens.Repositories.Contracts.WriteRepositories;

namespace DumaLens.Repositories.WriteRepositories;

/// <inheritdoc cref="IBillWriteRepository" />
public class BillWriteRepository : BaseWriteRepository<Bill>, IBillWriteRepository, IRepositoryAnchor
{
    /// <summary>
    /// Конструктор
    /// </summary>
    public BillWriteRepository(IWriter writer, IDateTimeProvider dateTimeProvider)
        : base(writer, dateTimeProvider)
    {
    }
}