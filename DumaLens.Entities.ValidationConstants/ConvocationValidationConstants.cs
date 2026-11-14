namespace DumaLens.Entities.ValidationConstants;

/// <summary>
/// Константы валидации созыва
/// </summary>
public static class ConvocationValidationConstants
{
    /// <summary>
    /// Минимальный номер созыва
    /// </summary>
    public const int NumberMin = 1;

    /// <summary>
    /// Максимальный номер созыва
    /// </summary>
    public const int NumberMax = 9;

    /// <summary>
    /// Максимальная длина кода созыва на сайте голосований
    /// </summary>
    public const int SiteCodeMaxLength = 16;
}