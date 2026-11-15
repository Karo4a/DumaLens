namespace DumaLens.Services.Contracts.Models;

/// <summary>
/// Фракционное условие поиска голосований
/// </summary>
public class FactionConditionQueryModel
{
    /// <summary>
    /// Фракция: код канонической фракции (ER), её название или историческое название («СР-Р», «НЛ»)
    /// </summary>
    public string Faction { get; set; } = string.Empty;

    /// <summary>
    /// Результат фракции (accept/decline/abstain/none, регистронезависимо)
    /// </summary>
    public string Result { get; set; } = string.Empty;

    /// <summary>
    /// Оператор сравнения (gte/lte, регистронезависимо); по умолчанию gte
    /// </summary>
    public string Operator { get; set; } = "gte";

    /// <summary>
    /// Порог процента (0–100)
    /// </summary>
    public decimal Threshold { get; set; }

    /// <summary>
    /// База процента (members/voted, регистронезависимо); по умолчанию members
    /// </summary>
    public string Basis { get; set; } = "members";
}