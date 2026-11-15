namespace DumaLens.Api.Models.Votes;

/// <summary>
/// Фракционное условие поиска голосований
/// </summary>
public class FactionConditionRequestApiModel
{
    /// <summary>
    /// Фракция: код, название или историческое название
    /// </summary>
    public string Faction { get; set; } = string.Empty;

    /// <summary>
    /// Результат фракции (токен: accept, decline, abstain, none)
    /// </summary>
    public string Result { get; set; } = string.Empty;

    /// <summary>
    /// Оператор сравнения (токен: gte, lte)
    /// </summary>
    public string Operator { get; set; } = "gte";

    /// <summary>
    /// Порог процента (0–100)
    /// </summary>
    public decimal Threshold { get; set; }

    /// <summary>
    /// База процента (токен: members, voted)
    /// </summary>
    public string Basis { get; set; } = "members";
}