using DumaLens.Common.Contracts;
using DumaLens.Context.Contracts;
using DumaLens.Entities;
using DumaLens.Repositories.Contracts.WriteRepositories;

namespace DumaLens.Repositories.WriteRepositories;

/// <inheritdoc cref="IUserWriteRepository" />
public class UserWriteRepository : BaseWriteRepository<User>, IUserWriteRepository, IRepositoryAnchor
{
    /// <summary>
    /// Конструктор
    /// </summary>
    public UserWriteRepository(IWriter writer, IDateTimeProvider dateTimeProvider)
        : base(writer, dateTimeProvider)
    {
    }
}