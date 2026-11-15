namespace DumaLens.Repositories.Contracts.Models;

/// <summary>
/// Модель законопроекта для запроса из базы данных
/// </summary>
public class BillDbModel
{
    /// <summary>
    /// Идентификатор
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Номер законопроекта (например, 500091-8)
    /// </summary>
    public string Number { get; set; } = string.Empty;
}