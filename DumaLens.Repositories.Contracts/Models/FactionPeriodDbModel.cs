namespace DumaLens.Repositories.Contracts.Models;

/// <summary>
/// Модель фракционного периода для запроса из базы данных
/// </summary>
public class FactionPeriodDbModel
{
    /// <summary>
    /// Идентификатор
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Идентификатор канонической фракции
    /// </summary>
    public Guid FactionId { get; set; }

    /// <summary>
    /// Идентификатор созыва
    /// </summary>
    public Guid ConvocationId { get; set; }

    /// <summary>
    /// Номер созыва
    /// </summary>
    public int ConvocationNumber { get; set; }

    /// <summary>
    /// Идентификатор фракции на сайте голосований
    /// </summary>
    public long SiteId { get; set; }

    /// <summary>
    /// Название фракции в этом созыве
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Дата начала периода
    /// </summary>
    public DateOnly DateFrom { get; set; }

    /// <summary>
    /// Дата окончания периода (null для действующего)
    /// </summary>
    public DateOnly? DateTo { get; set; }
}