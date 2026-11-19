namespace DumaLens.Services.Contracts.Models;

/// <summary>
/// Запрос смены пароля
/// </summary>
public class ChangePasswordModel
{
    /// <summary>
    /// Текущий пароль
    /// </summary>
    public string CurrentPassword { get; set; } = string.Empty;

    /// <summary>
    /// Новый пароль
    /// </summary>
    public string NewPassword { get; set; } = string.Empty;
}