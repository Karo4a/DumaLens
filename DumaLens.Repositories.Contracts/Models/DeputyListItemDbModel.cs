namespace DumaLens.Repositories.Contracts.Models;

/// <summary>
/// Депутат в списке по созыву
/// </summary>
public class DeputyListItemDbModel
{
    /// <summary>
    /// Идентификатор депутата на сайте Госдумы
    /// </summary>
    public long SiteId { get; set; }

    /// <summary>
    /// Полное имя депутата
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Имя депутата для сортировки (фамилия впереди)
    /// </summary>
    public string SortName { get; set; } = string.Empty;

    /// <summary>
    /// Коды фракций, в которых депутат состоял в созыве
    /// </summary>
    public IReadOnlyCollection<string> FactionCodes { get; set; } = [];
}