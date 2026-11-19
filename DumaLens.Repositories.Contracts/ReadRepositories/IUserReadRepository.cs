using DumaLens.Entities;
using DumaLens.Repositories.Contracts.Models;

namespace DumaLens.Repositories.Contracts.ReadRepositories;

/// <summary>
/// Репозиторий чтения сущности <see cref="User"/>
/// </summary>
public interface IUserReadRepository
{
    /// <summary>
    /// Получает <see cref="User"/> по логину (регистронезависимо, без удалённых)
    /// </summary>
    Task<UserDbModel?> GetByLogin(string login, CancellationToken cancellationToken);

    /// <summary>
    /// Получает <see cref="User"/> по идентификатору (без удалённых)
    /// </summary>
    Task<UserDbModel?> GetById(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Получает <see cref="User"/> по электронной почте (регистронезависимо, без удалённых)
    /// </summary>
    Task<UserDbModel?> GetByEmail(string email, CancellationToken cancellationToken);

    /// <summary>
    /// Получает страницу списка пользователей (без удалённых, по возрастанию логина)
    /// </summary>
    Task<UserPageDbModel> GetPage(int page, int pageSize, CancellationToken cancellationToken);

    /// <summary>
    /// Считает активных администраторов (не заблокированы, не удалены), исключая указанного пользователя
    /// </summary>
    Task<int> CountActiveAdministrators(Guid excludeUserId, CancellationToken cancellationToken);
}