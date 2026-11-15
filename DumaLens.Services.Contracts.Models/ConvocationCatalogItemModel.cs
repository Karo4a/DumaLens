namespace DumaLens.Services.Contracts.Models;

/// <summary>
/// Созыв в каталоге с числом голосований
/// </summary>
public class ConvocationCatalogItemModel
{
    /// <summary>
    /// Номер созыва
    /// </summary>
    public int Number { get; set; }

    /// <summary>
    /// Код созыва на сайте Госдумы
    /// </summary>
    public string SiteCode { get; set; } = string.Empty;

    /// <summary>
    /// Название созыва
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Дата начала работы созыва
    /// </summary>
    public DateOnly DateFrom { get; set; }

    /// <summary>
    /// Дата окончания работы созыва (null — созыв работает)
    /// </summary>
    public DateOnly? DateTo { get; set; }

    /// <summary>
    /// Число голосований в созыве
    /// </summary>
    public int VoteCount { get; set; }
}