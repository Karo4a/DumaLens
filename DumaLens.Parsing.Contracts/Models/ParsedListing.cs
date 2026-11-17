namespace DumaLens.Parsing.Contracts.Models;

/// <summary>
/// Результат разбора страницы листинга голосований
/// </summary>
public sealed class ParsedListing
{
    /// <summary>
    /// Элементы страницы (не более 20)
    /// </summary>
    public IReadOnlyList<ParsedListingItem> Items { get; set; } = [];

    /// <summary>
    /// Номера страниц, найденные в ссылках пагинации (по возрастанию, без повторов)
    /// </summary>
    public IReadOnlyList<int> PageNumbers { get; set; } = [];

    /// <summary>
    /// Замечания, выявленные при разборе страницы
    /// </summary>
    public IReadOnlyList<ParseIssue> Issues { get; set; } = [];

    /// <summary>
    /// Общее число голосований в листинге из счётчика «Показаны: с … по … из N» (null, если счётчик не найден)
    /// </summary>
    public int? TotalCount { get; set; }

    /// <summary>
    /// Номер последней страницы пагинации (1, если ссылок нет)
    /// </summary>
    public int LastPageNumber => PageNumbers.Count == 0 ? 1 : PageNumbers[^1];
}