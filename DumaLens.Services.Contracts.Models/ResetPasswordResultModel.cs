namespace DumaLens.Services.Contracts.Models;

/// <summary>
/// Результат сброса пароля: временный пароль для передачи пользователю
/// </summary>
public class ResetPasswordResultModel
{
    /// <summary>
    /// Сгенерированный временный пароль (показывается администратору один раз)
    /// </summary>
    public string TemporaryPassword { get; set; } = string.Empty;
}