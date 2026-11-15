using DumaLens.Entities.Contracts.Enums;
using DumaLens.Repositories.Contracts.Enums;

namespace DumaLens.Repositories.Contracts.Models;

/// <summary>
/// Фракционное условие поиска голосований, разрешённое до идентификатора канонической фракции
/// </summary>
public class VoteFactionConditionSpec
{
    /// <summary>
    /// Идентификатор канонической фракции
    /// </summary>
    public Guid FactionId { get; set; }

    /// <summary>
    /// Результат фракции в голосовании
    /// </summary>
    public VoteResult Result { get; set; }

    /// <summary>
    /// Оператор сравнения процента с порогом
    /// </summary>
    public ComparisonOperator Operator { get; set; }

    /// <summary>
    /// Порог процента (0–100)
    /// </summary>
    public decimal Threshold { get; set; }

    /// <summary>
    /// База процента
    /// </summary>
    public PercentBasis Basis { get; set; }
}