using DumaLens.Context.Contracts;
using DumaLens.Entities;
using DumaLens.Entities.Enums;
using DumaLens.Repositories.Contracts.Models;
using DumaLens.Repositories.Contracts.ReadRepositories;
using Microsoft.EntityFrameworkCore;

namespace DumaLens.Repositories.ReadRepositories;

/// <inheritdoc cref="IUserRoleReadRepository" />
public class UserRoleReadRepository : IUserRoleReadRepository, IRepositoryAnchor
{
    private readonly IReader reader;

    /// <summary>
    /// Конструктор
    /// </summary>
    public UserRoleReadRepository(IReader reader)
    {
        this.reader = reader;
    }

    Task<UserRoleDbModel?> IUserRoleReadRepository.GetById(Guid id, CancellationToken cancellationToken)
        => reader.Read<UserRole>()
            .NotDeletedAt()
            .ById(id)
            .Select(x => new UserRoleDbModel
            {
                Id = x.Id,
                Role = x.Role,
                Description = x.Description,
            })
            .FirstOrDefaultAsync(cancellationToken);

    Task<UserRoleDbModel?> IUserRoleReadRepository.GetByRole(Role role, CancellationToken cancellationToken)
        => reader.Read<UserRole>()
            .NotDeletedAt()
            .Where(x => x.Role == role)
            .Select(x => new UserRoleDbModel
            {
                Id = x.Id,
                Role = x.Role,
                Description = x.Description,
            })
            .FirstOrDefaultAsync(cancellationToken);

    Task<IReadOnlyCollection<UserRoleDbModel>> IUserRoleReadRepository.GetAll(CancellationToken cancellationToken)
        => reader.Read<UserRole>()
            .NotDeletedAt()
            .Select(x => new UserRoleDbModel
            {
                Id = x.Id,
                Role = x.Role,
                Description = x.Description,
            })
            .OrderBy(x => x.Role)
            .ToReadOnlyCollectionAsync(cancellationToken);
}