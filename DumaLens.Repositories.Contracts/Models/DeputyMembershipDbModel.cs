namespace DumaLens.Repositories.Contracts.Models;

/// <summary>
/// Членство депутата во фракции в рамках созыва
/// </summary>
public class DeputyMembershipDbModel
{
    /// <summary>
    /// Номер созыва
    /// </summary>
    public int ConvocationNumber { get; set; }

    /// <summary>
    /// Код канонической фракции
    /// </summary>
    public string FactionCode { get; set; } = string.Empty;

    /// <summary>
    /// Название канонической фракции
    /// </summary>
    public string FactionTitle { get; set; } = string.Empty;

    /// <summary>
    /// Историческое название фракции в созыве
    /// </summary>
    public string PeriodTitle { get; set; } = string.Empty;

    /// <summary>
    /// Дата начала членства (null — граница неизвестна)
    /// </summary>
    public DateOnly? DateFrom { get; set; }

    /// <summary>
    /// Дата окончания членства (null — граница неизвестна или членство открыто)
    /// </summary>
    public DateOnly? DateTo { get; set; }
}