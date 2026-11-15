namespace DumaLens.Services.Contracts.Models;

/// <summary>
/// Исторический период фракции в созыве
/// </summary>
public class FactionPeriodModel
{
    /// <summary>
    /// Номер созыва
    /// </summary>
    public int ConvocationNumber { get; set; }

    /// <summary>
    /// Идентификатор фракции созыва на сайте Госдумы
    /// </summary>
    public long SiteId { get; set; }

    /// <summary>
    /// Историческое название фракции в созыве
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Дата начала периода
    /// </summary>
    public DateOnly DateFrom { get; set; }

    /// <summary>
    /// Дата окончания периода (null — период открыт)
    /// </summary>
    public DateOnly? DateTo { get; set; }
}