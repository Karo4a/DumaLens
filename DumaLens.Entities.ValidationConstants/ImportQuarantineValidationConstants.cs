namespace DumaLens.Entities.ValidationConstants;

/// <summary>
/// Константы валидации записи карантина импорта
/// </summary>
public static class ImportQuarantineValidationConstants
{
    /// <summary>
    /// Максимальная длина пути файла-источника
    /// </summary>
    public const int SourceFileMaxLength = 512;

    /// <summary>
    /// Максимальная длина URL источника
    /// </summary>
    public const int SourceUrlMaxLength = 2048;

    /// <summary>
    /// Максимальная длина причины помещения в карантин
    /// </summary>
    public const int ReasonMaxLength = 1024;
}