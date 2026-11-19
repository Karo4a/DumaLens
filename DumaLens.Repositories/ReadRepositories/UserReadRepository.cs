using DumaLens.Context.Contracts;
using DumaLens.Entities;
using DumaLens.Entities.Enums;
using DumaLens.Repositories.Contracts.Models;
using DumaLens.Repositories.Contracts.ReadRepositories;
using Microsoft.EntityFrameworkCore;

namespace DumaLens.Repositories.ReadRepositories;

/// <inheritdoc cref="IUserReadRepository" />
public class UserReadRepository : IUserReadRepository, IRepositoryAnchor
{
    private readonly IReader reader;

    /// <summary>
    /// Конструктор
    /// </summary>
    public UserReadRepository(IReader reader)
    {
        this.reader = reader;
    }

    Task<UserDbModel?> IUserReadRepository.GetByLogin(string login, CancellationToken cancellationToken)
    {
        var loginLowerCase = login.ToLowerInvariant();
        return Project(reader.Read<User>()
                .NotDeletedAt()
                .Where(x => x.LoginLowerCase == loginLowerCase))
            .FirstOrDefaultAsync(cancellationToken);
    }

    Task<UserDbModel?> IUserReadRepository.GetById(Guid id, CancellationToken cancellationToken)
        => Project(reader.Read<User>()
                .NotDeletedAt()
                .ById(id))
            .FirstOrDefaultAsync(cancellationToken);

    Task<UserDbModel?> IUserReadRepository.GetByEmail(string email, CancellationToken cancellationToken)
    {
        var emailLowerCase = email.ToLowerInvariant();
        return Project(reader.Read<User>()
                .NotDeletedAt()
                .Where(x => x.EmailLowerCase == emailLowerCase))
            .FirstOrDefaultAsync(cancellationToken);
    }

    async Task<UserPageDbModel> IUserReadRepository.GetPage(int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = reader.Read<User>()
            .NotDeletedAt()
            .OrderBy(x => x.LoginLowerCase);
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new UserListItemDbModel
            {
                Id = x.Id,
                Login = x.Login,
                Email = x.Email,
                Name = x.Name,
                Role = x.Role.Role,
                Blocked = x.Blocked,
                PasswordIsTemporary = x.PasswordIsTemporary,
                LoginAttemptFailedCount = x.LoginAttemptFailedCount,
                Note = x.Note,
                CreatedAt = x.CreatedAt,
            })
            .ToReadOnlyCollectionAsync(cancellationToken);
        return new UserPageDbModel
        {
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            Items = items,
        };
    }

    Task<int> IUserReadRepository.CountActiveAdministrators(Guid excludeUserId, CancellationToken cancellationToken)
        => reader.Read<User>()
            .NotDeletedAt()
            .Where(x => x.Id != excludeUserId && !x.Blocked && x.Role.Role == Role.Administrator)
            .CountAsync(cancellationToken);

    private static IQueryable<UserDbModel> Project(IQueryable<User> query)
        => query.Select(x => new UserDbModel
        {
            Id = x.Id,
            Login = x.Login,
            Email = x.Email,
            Name = x.Name,
            PasswordHash = x.PasswordHash,
            PasswordSalt = x.PasswordSalt,
            PasswordIsTemporary = x.PasswordIsTemporary,
            SecurityStamp = x.SecurityStamp,
            Role = x.Role.Role,
            RoleId = x.RoleId,
            Blocked = x.Blocked,
            LoginAttemptFailedCount = x.LoginAttemptFailedCount,
            LastFailedLoginAt = x.LastFailedLoginAt,
            Note = x.Note,
            CreatedAt = x.CreatedAt,
        });
}