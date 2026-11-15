using DumaLens.Entities.ValidationConstants;

namespace DumaLens.Services.Contracts.Models;

/// <summary>
/// Запрос поиска голосований
/// </summary>
/// <remarks>
/// Стадия, исход, сортировка и фракционные токены передаются строками контракта API
/// и валидируются сервисом (неизвестные токены — ошибка валидации, а не привязки модели).
/// </remarks>
public class VoteSearchQueryModel
{
    /// <summary>
    /// Текстовый запрос (полнотекстовый поиск, websearch-синтаксис); null — без текстового фильтра
    /// </summary>
    public string? Query { get; set; }

    /// <summary>
    /// Номер созыва (1–9); null — без фильтра
    /// </summary>
    public int? Convocation { get; set; }

    /// <summary>
    /// Нижняя граница даты голосования (московская дата, включительно); null — без фильтра
    /// </summary>
    public DateOnly? DateFrom { get; set; }

    /// <summary>
    /// Верхняя граница даты голосования (московская дата, включительно); null — без фильтра
    /// </summary>
    public DateOnly? DateTo { get; set; }

    /// <summary>
    /// Точный номер законопроекта; null — без фильтра
    /// </summary>
    public string? BillNumber { get; set; }

    /// <summary>
    /// Стадия голосования (имя перечисления VoteStage, регистронезависимо); null — без фильтра
    /// </summary>
    public string? Stage { get; set; }

    /// <summary>
    /// Исход голосования (accepted/rejected/unknown, регистронезависимо); null — без фильтра
    /// </summary>
    public string? Outcome { get; set; }

    /// <summary>
    /// Наличие результатов голосования; null — по умолчанию true при фракционных условиях, иначе без фильтра
    /// </summary>
    public bool? HasResults { get; set; }

    /// <summary>
    /// Сортировка (dateDesc/dateAsc/relevance/acceptCountDesc); null — dateDesc
    /// </summary>
    public string? Sort { get; set; }

    /// <summary>
    /// Номер страницы (с 1)
    /// </summary>
    public int Page { get; set; } = VoteSearchValidationConstants.PageMin;

    /// <summary>
    /// Размер страницы (1–100)
    /// </summary>
    public int PageSize { get; set; } = VoteSearchValidationConstants.PageSizeDefault;

    /// <summary>
    /// Фракционные условия, объединяемые по И (не более 10)
    /// </summary>
    public IReadOnlyCollection<FactionConditionQueryModel> FactionConditions { get; set; } = [];
}