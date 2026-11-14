namespace DumaLens.Entities.ValidationConstants;

/// <summary>
/// Константы валидации голосования
/// </summary>
public static class VoteValidationConstants
{
    /// <summary>
    /// Максимальная длина исходного префикса стадии из заголовка
    /// </summary>
    public const int StageRawMaxLength = 512;

    /// <summary>
    /// Максимальная длина строкового значения стадии
    /// </summary>
    public const int StageMaxLength = 64;

    /// <summary>
    /// Максимальная длина строкового значения итога голосования
    /// </summary>
    public const int OutcomeMaxLength = 32;

    /// <summary>
    /// Максимальная длина строкового значения источника данных
    /// </summary>
    public const int SourceMaxLength = 16;
}