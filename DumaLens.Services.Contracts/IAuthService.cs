using DumaLens.Services.Contracts.Models;

namespace DumaLens.Services.Contracts;

/// <summary>
/// Сервис аутентификации: вход, обновление и отзыв сессий, смена пароля (07-security.md §2)
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Выполняет вход по логину и паролю: пара токенов и профиль; учитывает блокировку и счётчик неудачных попыток
    /// </summary>
    Task<AuthResultModel> Login(LoginModel model, CancellationToken cancellationToken);

    /// <summary>
    /// Обновляет пару токенов по refresh-токену (ротация); повторное использование отозванного отзывает все сессии
    /// </summary>
    Task<AuthResultModel> Refresh(RefreshRequestModel model, CancellationToken cancellationToken);

    /// <summary>
    /// Выполняет выход: отзывает активный refresh-токен пользователя
    /// </summary>
    Task Logout(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Меняет пароль: проверяет текущий, меняет метку безопасности и отзывает все сессии
    /// </summary>
    Task ChangePassword(Guid userId, ChangePasswordModel model, CancellationToken cancellationToken);

    /// <summary>
    /// Возвращает профиль текущего пользователя
    /// </summary>
    Task<UserProfileModel> GetProfile(Guid userId, CancellationToken cancellationToken);
}