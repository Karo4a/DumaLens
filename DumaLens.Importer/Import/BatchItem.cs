using DumaLens.Parsing.Contracts.Models;
using DumaLens.Repositories.Contracts.Models;

namespace DumaLens.Importer.Import;

/// <summary>
/// Элемент пачки импорта: разобранный файл голосования, годный к загрузке либо подлежащий карантину
/// </summary>
public sealed class BatchItem
{
    private BatchItem(
        string entryName,
        string rawJson,
        ParsedVote parsed,
        NormalizedTitle? normalized,
        ConvocationDbModel? convocation,
        IReadOnlyList<string> quarantineReasons)
    {
        EntryName = entryName;
        RawJson = rawJson;
        Parsed = parsed;
        Normalized = normalized;
        Convocation = convocation;
        QuarantineReasons = quarantineReasons;
    }

    /// <summary>
    /// Имя файла в архиве
    /// </summary>
    public string EntryName { get; }

    /// <summary>
    /// Исходный JSON файла
    /// </summary>
    public string RawJson { get; }

    /// <summary>
    /// Результат разбора файла
    /// </summary>
    public ParsedVote Parsed { get; }

    /// <summary>
    /// Нормализованный заголовок (для годных файлов)
    /// </summary>
    public NormalizedTitle? Normalized { get; }

    /// <summary>
    /// Созыв, в который попадает дата голосования (для годных файлов)
    /// </summary>
    public ConvocationDbModel? Convocation { get; }

    /// <summary>
    /// Причины карантина; пустой список — файл годен к загрузке
    /// </summary>
    public IReadOnlyList<string> QuarantineReasons { get; }

    /// <summary>
    /// Признак того, что файл годен к загрузке
    /// </summary>
    public bool IsValid => QuarantineReasons.Count == 0;

    /// <summary>
    /// Создать годный элемент пачки
    /// </summary>
    public static BatchItem Valid(string entryName, string rawJson, ParsedVote parsed, NormalizedTitle normalized, ConvocationDbModel convocation)
    {
        return new BatchItem(entryName, rawJson, parsed, normalized, convocation, Array.Empty<string>());
    }

    /// <summary>
    /// Создать элемент пачки, подлежащий карантину
    /// </summary>
    public static BatchItem Quarantined(string entryName, string rawJson, ParsedVote parsed, IReadOnlyList<string> reasons)
    {
        return new BatchItem(entryName, rawJson, parsed, null, null, reasons);
    }
}