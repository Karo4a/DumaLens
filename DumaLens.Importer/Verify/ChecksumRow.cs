namespace DumaLens.Importer.Verify;

/// <summary>
/// Строка сверки контрольной суммы: ожидаемое и фактическое значения
/// </summary>
public sealed class ChecksumRow
{
    /// <summary>
    /// Название метрики
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Ожидаемое значение по паспорту архива
    /// </summary>
    public long Expected { get; set; }

    /// <summary>
    /// Фактическое значение в базе
    /// </summary>
    public long Actual { get; set; }

    /// <summary>
    /// Признак совпадения ожидаемого и фактического значений
    /// </summary>
    public bool IsMatch => Expected == Actual;
}