namespace DumaLens.Api.Models.Auth;

/// <summary>
/// Запрос смены пароля
/// </summary>
public class ChangePasswordRequestApiModel
{
    /// <summary>
    /// Текущий пароль
    /// </summary>
    public string CurrentPassword { get; set; } = string.Empty;

    /// <summary>
    /// Новый пароль (не менее 8 символов)
    /// </summary>
    public string NewPassword { get; set; } = string.Empty;
}