namespace DumaLens.Repositories.Contracts.Models;

/// <summary>
/// Модель созыва для запроса из базы данных
/// </summary>
public class ConvocationDbModel
{
    /// <summary>
    /// Идентификатор
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Номер созыва (1–9)
    /// </summary>
    public int Number { get; set; }

    /// <summary>
    /// Код созыва на сайте голосований
    /// </summary>
    public string SiteCode { get; set; } = string.Empty;

    /// <summary>
    /// Название созыва
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Дата начала работы созыва
    /// </summary>
    public DateOnly DateFrom { get; set; }

    /// <summary>
    /// Дата окончания работы созыва (null для действующего созыва)
    /// </summary>
    public DateOnly? DateTo { get; set; }
}