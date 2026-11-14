using DumaLens.Entities.Contracts;

namespace DumaLens.Entities;

/// <summary>
/// Запись карантина импорта
/// </summary>
/// <remarks>
/// Аномалии разбора и импорта: сырой payload сохраняется для ручного разбора, в основную базу не попадает.
/// </remarks>
public class ImportQuarantine : DbBaseEntity
{
    /// <summary>
    /// Файл-источник (путь внутри архива)
    /// </summary>
    public string? SourceFile { get; set; }

    /// <summary>
    /// URL источника (живой сайт)
    /// </summary>
    public string? SourceUrl { get; set; }

    /// <summary>
    /// Идентификатор голосования на сайте (если известен)
    /// </summary>
    public long? SiteId { get; set; }

    /// <summary>
    /// Причина помещения в карантин
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Сырой JSON источника
    /// </summary>
    public string RawJson { get; set; } = string.Empty;
}