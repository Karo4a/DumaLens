using DumaLens.Entities;
using DumaLens.Repositories.Contracts.Models;

namespace DumaLens.Repositories.Contracts.ReadRepositories;

/// <summary>
/// Репозиторий чтения сущности <see cref="Convocation"/>
/// </summary>
public interface IConvocationReadRepository
{
    /// <summary>
    /// Получает <see cref="Convocation"/> по идентификатору
    /// </summary>
    Task<ConvocationDbModel?> GetById(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Получает <see cref="Convocation"/> по номеру
    /// </summary>
    Task<ConvocationDbModel?> GetByNumber(int number, CancellationToken cancellationToken);

    /// <summary>
    /// Получает коллекцию <see cref="Convocation"/>, упорядоченную по номеру
    /// </summary>
    Task<IReadOnlyCollection<ConvocationDbModel>> GetAll(CancellationToken cancellationToken);
}