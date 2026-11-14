namespace DumaLens.Entities.ValidationConstants;

/// <summary>
/// Общие константы валидации
/// </summary>
public static class CommonValidationConstants
{
    /// <summary>
    /// Максимальная длина названия справочника
    /// </summary>
    public const int TitleMaxLength = 512;

    /// <summary>
    /// Тип колонок процентов (точность 5, масштаб 2)
    /// </summary>
    public const string PercentColumnType = "numeric(5,2)";

    /// <summary>
    /// Минимальное значение процента
    /// </summary>
    public const decimal PercentMin = 0m;

    /// <summary>
    /// Максимальное значение процента
    /// </summary>
    public const decimal PercentMax = 100m;
}