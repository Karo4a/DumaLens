using DumaLens.Entities.Contracts.Enums;
using DumaLens.Repositories.Contracts.Enums;

namespace DumaLens.Repositories.Contracts.Models;

/// <summary>
/// Спецификация поиска голосований: все ссылки разрешены сервисом до идентификаторов
/// </summary>
public class VoteSearchSpec
{
    /// <summary>
    /// Текст полнотекстового запроса (обрезанный, непустой); null — без текстового фильтра
    /// </summary>
    /// <remarks>Поддерживается только PostgreSQL (ADR-1); на InMemory не используется</remarks>
    public string? Query { get; set; }

    /// <summary>
    /// Идентификатор созыва; null — без фильтра
    /// </summary>
    public Guid? ConvocationId { get; set; }

    /// <summary>
    /// Нижняя граница даты голосования (московская дата, включительно); null — без фильтра
    /// </summary>
    public DateOnly? DateFrom { get; set; }

    /// <summary>
    /// Верхняя граница даты голосования (московская дата, включительно); null — без фильтра
    /// </summary>
    public DateOnly? DateTo { get; set; }

    /// <summary>
    /// Идентификатор законопроекта; null — без фильтра
    /// </summary>
    public Guid? BillId { get; set; }

    /// <summary>
    /// Стадия голосования; null — без фильтра
    /// </summary>
    public VoteStage? Stage { get; set; }

    /// <summary>
    /// Исход голосования; null — без фильтра
    /// </summary>
    public VoteOutcome? Outcome { get; set; }

    /// <summary>
    /// Наличие результатов голосования; null — без фильтра
    /// </summary>
    public bool? HasResults { get; set; }

    /// <summary>
    /// Фракционные условия, объединяемые по И
    /// </summary>
    public IReadOnlyCollection<VoteFactionConditionSpec> FactionConditions { get; set; } = [];

    /// <summary>
    /// Сортировка выдачи (по умолчанию <see cref="VoteSearchSort.DateDesc"/>)
    /// </summary>
    public VoteSearchSort Sort { get; set; }

    /// <summary>
    /// Номер страницы (с 1); задаётся сервисом после валидации
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Размер страницы; задаётся сервисом после валидации
    /// </summary>
    public int PageSize { get; set; }
}