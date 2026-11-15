namespace DumaLens.Services.Contracts.Models;

/// <summary>
/// Запрос поиска законопроектов по части номера
/// </summary>
public class BillSearchQueryModel
{
    /// <summary>
    /// Часть номера законопроекта
    /// </summary>
    public string NumberPart { get; set; } = string.Empty;
}