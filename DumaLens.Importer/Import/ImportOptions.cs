namespace DumaLens.Importer.Import;

/// <summary>
/// Параметры импорта архива
/// </summary>
public sealed class ImportOptions
{
    /// <summary>
    /// Путь к архиву vote-master.zip
    /// </summary>
    public string ZipPath { get; set; } = string.Empty;

    /// <summary>
    /// Размер пачки голосований в одной транзакции
    /// </summary>
    public int BatchSize { get; set; } = 500;

    /// <summary>
    /// Пропускать голосования, уже загруженные в базу (продолжение после сбоя)
    /// </summary>
    public bool Resume { get; set; }
}