namespace DumaLens.Services.Contracts.Exceptions;

/// <summary>
/// Базовый класс доменных исключений
/// </summary>
public class DumaLensException : Exception
{
    /// <summary>
    /// Конструктор без параметров
    /// </summary>
    public DumaLensException() { }

    /// <summary>
    /// Конструктор c указанием сообщения об ошибке
    /// </summary>
    public DumaLensException(string message)
        : base(message) { }
}