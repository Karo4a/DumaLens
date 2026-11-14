namespace DumaLens.Entities.Contracts.Enums;

/// <summary>
/// Результат индивидуального голоса депутата
/// </summary>
public enum VoteResult
{
    /// <summary>
    /// За
    /// </summary>
    Accept = 0,
    /// <summary>
    /// Против
    /// </summary>
    Decline = 1,
    /// <summary>
    /// Воздержался
    /// </summary>
    Abstain = 2,
    /// <summary>
    /// Не голосовал
    /// </summary>
    None = 3
}