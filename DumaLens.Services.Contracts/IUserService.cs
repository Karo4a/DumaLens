using DumaLens.Services.Contracts.Models;

namespace DumaLens.Services.Contracts;

/// <summary>
/// Сервис управления пользователями (Администратор, 07-security.md §2.4)
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Возвращает страницу списка пользователей
    /// </summary>
    Task<UserPageModel> GetPage(UserListQueryModel query, CancellationToken cancellationToken);

    /// <summary>
    /// Создаёт пользователя с указанной ролью и временным паролем
    /// </summary>
    Task<UserListItemModel> Create(CreateUserModel model, CancellationToken cancellationToken);

    /// <summary>
    /// Обновляет профиль, роль и блокировку пользователя; смена роли или блокировка отзывает сессии
    /// </summary>
    Task<UserListItemModel> Update(Guid id, UpdateUserModel model, CancellationToken cancellationToken);

    /// <summary>
    /// Сбрасывает пароль: генерирует временный пароль, меняет метку безопасности и отзывает все сессии
    /// </summary>
    Task<ResetPasswordResultModel> ResetPassword(Guid id, CancellationToken cancellationToken);
}