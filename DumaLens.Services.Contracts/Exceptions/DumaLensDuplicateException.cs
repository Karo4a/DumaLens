namespace DumaLens.Services.Contracts.Exceptions;

/// <summary>
/// Исключение найденного повторения
/// </summary>
public class DumaLensDuplicateException : DumaLensException
{
    /// <summary>
    /// Конструктор
    /// </summary>
    public DumaLensDuplicateException(string message)
        : base(message) { }
}