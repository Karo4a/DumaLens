using DumaLens.Entities.Contracts;

namespace DumaLens.Entities;

/// <summary>
/// Законопроект
/// </summary>
/// <remarks>
/// Связь с голосованиями — только по номеру из заголовка; тексты и авторы — вне MVP (СОЗД).
/// </remarks>
public class Bill : DbBaseEntity
{
    /// <summary>
    /// Префикс ссылки на карточку законопроекта в СОЗД
    /// </summary>
    public const string SozdUrlPrefix = "https://sozd.duma.gov.ru/bill/";

    /// <summary>
    /// Номер законопроекта (например, 500091-8)
    /// </summary>
    public string Number { get; set; } = string.Empty;

    /// <summary>
    /// Ссылка на карточку законопроекта в СОЗД (вычисляемая, не хранится в БД)
    /// </summary>
    public string SozdUrl => SozdUrlPrefix + Number;
}