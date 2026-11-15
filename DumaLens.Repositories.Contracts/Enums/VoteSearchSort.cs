namespace DumaLens.Repositories.Contracts.Enums;

/// <summary>
/// Сортировка выдачи поиска голосований
/// </summary>
public enum VoteSearchSort
{
    /// <summary>
    /// По дате голосования, новые сначала (по умолчанию)
    /// </summary>
    DateDesc = 0,

    /// <summary>
    /// По дате голосования, старые сначала
    /// </summary>
    DateAsc = 1,

    /// <summary>
    /// По релевантности текстового запроса (только при непустом запросе)
    /// </summary>
    Relevance = 2,

    /// <summary>
    /// По числу голосов «за», больше сначала
    /// </summary>
    AcceptCountDesc = 3,
}