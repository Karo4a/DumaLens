namespace DumaLens.Services.Contracts.Models;

/// <summary>
/// Законопроект в результатах поиска по части номера
/// </summary>
public class BillSearchItemModel
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