namespace DumaLens.Entities.ValidationConstants;

/// <summary>
/// Константы валидации поиска голосований
/// </summary>
public static class VoteSearchValidationConstants
{
    /// <summary>
    /// Минимальный номер страницы
    /// </summary>
    public const int PageMin = 1;

    /// <summary>
    /// Максимальный номер страницы (защита Skip от переполнения int)
    /// </summary>
    public const int PageMax = 100_000;

    /// <summary>
    /// Размер страницы по умолчанию
    /// </summary>
    public const int PageSizeDefault = 20;

    /// <summary>
    /// Минимальный размер страницы выдачи
    /// </summary>
    public const int PageSizeMin = 1;

    /// <summary>
    /// Максимальный размер страницы выдачи
    /// </summary>
    public const int PageSizeMax = 100;

    /// <summary>
    /// Максимальное число фракционных условий в одном запросе
    /// </summary>
    public const int FactionConditionsMax = 10;

    /// <summary>
    /// Максимальная длина текстового запроса
    /// </summary>
    public const int QueryMaxLength = 512;

    /// <summary>
    /// Минимальный номер созыва
    /// </summary>
    public const int ConvocationNumberMin = 1;

    /// <summary>
    /// Максимальный номер созыва
    /// </summary>
    public const int ConvocationNumberMax = 9;
}