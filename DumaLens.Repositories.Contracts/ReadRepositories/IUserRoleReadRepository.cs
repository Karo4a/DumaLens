using DumaLens.Entities;
using DumaLens.Entities.Enums;
using DumaLens.Repositories.Contracts.Models;

namespace DumaLens.Repositories.Contracts.ReadRepositories;

/// <summary>
/// Репозиторий чтения сущности <see cref="UserRole"/>
/// </summary>
public interface IUserRoleReadRepository
{
    /// <summary>
    /// Получает <see cref="UserRole"/> по идентификатору
    /// </summary>
    Task<UserRoleDbModel?> GetById(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Получает <see cref="UserRole"/> по роли
    /// </summary>
    Task<UserRoleDbModel?> GetByRole(Role role, CancellationToken cancellationToken);

    /// <summary>
    /// Получает коллекцию <see cref="UserRole"/>, упорядоченную по роли
    /// </summary>
    Task<IReadOnlyCollection<UserRoleDbModel>> GetAll(CancellationToken cancellationToken);
}