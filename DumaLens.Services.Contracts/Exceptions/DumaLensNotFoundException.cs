namespace DumaLens.Services.Contracts.Exceptions;

/// <summary>
/// Исключение не найденной записи
/// </summary>
public class DumaLensNotFoundException : DumaLensException
{
    /// <summary>
    /// Конструктор
    /// </summary>
    public DumaLensNotFoundException(string message)
        : base(message) { }
}