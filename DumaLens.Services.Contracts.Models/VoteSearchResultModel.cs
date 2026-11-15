namespace DumaLens.Services.Contracts.Models;

/// <summary>
/// Результат поиска голосований (страница выдачи)
/// </summary>
public class VoteSearchResultModel
{
    /// <summary>
    /// Общее число голосований, удовлетворяющих условиям (до пагинации)
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Номер текущей страницы (с 1)
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Размер страницы
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Строки текущей страницы
    /// </summary>
    public IReadOnlyCollection<VoteSearchItemModel> Items { get; set; } = [];
}