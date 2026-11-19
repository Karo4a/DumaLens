namespace DumaLens.Api.Models.Auth;

/// <summary>
/// Запрос входа по логину и паролю
/// </summary>
public class LoginRequestApiModel
{
    /// <summary>
    /// Логин
    /// </summary>
    public string Login { get; set; } = string.Empty;

    /// <summary>
    /// Пароль
    /// </summary>
    public string Password { get; set; } = string.Empty;
}