namespace DumaLens.Services.Contracts.Exceptions;

/// <summary>
/// Ошибки валидации
/// </summary>
public class DumaLensValidationException : DumaLensException
{
    /// <summary>
    /// Список ошибок
    /// </summary>
    public IEnumerable<InvalidateItemModel> Errors { get; }

    /// <summary>
    /// Конструктор
    /// </summary>
    public DumaLensValidationException(IEnumerable<InvalidateItemModel> errors)
    {
        Errors = errors;
    }
}