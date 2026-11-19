namespace DumaLens.Services.Contracts.Exceptions;

/// <summary>
/// Исключение неавторизованного доступа (неверные учётные данные, отозванный или просроченный токен)
/// </summary>
public class DumaLensUnauthorizedException : DumaLensException
{
    /// <summary>
    /// Конструктор
    /// </summary>
    public DumaLensUnauthorizedException(string message)
        : base(message) { }
}