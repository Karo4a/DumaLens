using DumaLens.Entities.ValidationConstants;

namespace DumaLens.Api.Models.Votes;

/// <summary>
/// Запрос поиска голосований
/// </summary>
public class SearchVotesRequestApiModel
{
    /// <summary>
    /// Текстовый запрос (полнотекстовый поиск по заголовкам)
    /// </summary>
    public string? Q { get; set; }

    /// <summary>
    /// Номер созыва
    /// </summary>
    public int? Convocation { get; set; }

    /// <summary>
    /// Начало диапазона дат голосования (московское время)
    /// </summary>
    public DateOnly? DateFrom { get; set; }

    /// <summary>
    /// Конец диапазона дат голосования (московское время)
    /// </summary>
    public DateOnly? DateTo { get; set; }

    /// <summary>
    /// Номер законопроекта
    /// </summary>
    public string? BillNumber { get; set; }

    /// <summary>
    /// Стадия рассмотрения (токен: FirstReading, ThirdReading, ...)
    /// </summary>
    public string? Stage { get; set; }

    /// <summary>
    /// Итог голосования (токен: accepted, rejected, unknown)
    /// </summary>
    public string? Outcome { get; set; }

    /// <summary>
    /// Сортировка (токен: dateDesc, dateAsc, relevance, acceptCountDesc)
    /// </summary>
    public string? Sort { get; set; }

    /// <summary>
    /// Номер страницы
    /// </summary>
    public int Page { get; set; } = VoteSearchValidationConstants.PageMin;

    /// <summary>
    /// Размер страницы
    /// </summary>
    public int PageSize { get; set; } = VoteSearchValidationConstants.PageSizeDefault;

    /// <summary>
    /// Фракционные условия (комбинируются И)
    /// </summary>
    public ICollection<FactionConditionRequestApiModel> FactionConditions { get; set; } = [];
}