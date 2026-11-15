namespace DumaLens.Api.Models.Votes;

/// <summary>
/// Страница результатов поиска голосований
/// </summary>
public class VoteSearchResultApiModel
{
    /// <summary>
    /// Общее число найденных голосований
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Номер страницы
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Размер страницы
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Голосования на странице
    /// </summary>
    public IReadOnlyCollection<VoteApiModel> Items { get; set; } = [];
}