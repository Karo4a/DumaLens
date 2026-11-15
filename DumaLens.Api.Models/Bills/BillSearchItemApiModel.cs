namespace DumaLens.Api.Models.Bills;

/// <summary>
/// Законопроект в результатах поиска по части номера
/// </summary>
public class BillSearchItemApiModel
{
    /// <summary>
    /// Номер законопроекта
    /// </summary>
    public string Number { get; set; } = string.Empty;

    /// <summary>
    /// Ссылка на карточку законопроекта в СОЗД
    /// </summary>
    public string SozdUrl { get; set; } = string.Empty;
}