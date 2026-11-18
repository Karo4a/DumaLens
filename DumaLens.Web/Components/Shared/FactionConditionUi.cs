namespace DumaLens.Web.Components.Shared;

/// <summary>
/// Фракционное условие поиска в состоянии конструктора (токены API)
/// </summary>
public sealed class FactionConditionUi
{
    /// <summary>
    /// Код канонической фракции
    /// </summary>
    public string Faction { get; set; } = "ER";

    /// <summary>
    /// Результат фракции: accept, decline, abstain, none
    /// </summary>
    public string Result { get; set; } = "accept";

    /// <summary>
    /// Оператор сравнения: gte, lte
    /// </summary>
    public string Operator { get; set; } = "gte";

    /// <summary>
    /// Порог процента (0–100)
    /// </summary>
    public double Threshold { get; set; } = 50;

    /// <summary>
    /// База процента: members, voted (для none только members)
    /// </summary>
    public string Basis { get; set; } = "members";
}