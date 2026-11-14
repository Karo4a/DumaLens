using DumaLens.Entities.Contracts;

namespace DumaLens.Entities;

/// <summary>
/// Депутат Государственной Думы
/// </summary>
/// <remarks>
/// Идентификатор сайта стабилен между созывами: один человек — одна запись; членство во фракциях историческое.
/// </remarks>
public class Deputy : DbBaseEntity
{
    /// <summary>
    /// Идентификатор депутата на сайте голосований
    /// </summary>
    public long SiteId { get; set; }

    /// <summary>
    /// Полное имя (ФИО)
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Имя для сортировки (sortName с сайта)
    /// </summary>
    public string SortName { get; set; } = string.Empty;
}