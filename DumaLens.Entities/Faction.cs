using DumaLens.Entities.Contracts;

namespace DumaLens.Entities;

/// <summary>
/// Каноническая фракция
/// </summary>
/// <remarks>
/// Исторические названия и site-id фракции живут в <see cref="FactionPeriod"/> (ADR-8).
/// </remarks>
public class Faction : DbBaseEntity
{
    /// <summary>
    /// Код канонической фракции (ER, KPRF, LDPR, SR, NL, INDEPENDENT, …)
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Название фракции
    /// </summary>
    public string Title { get; set; } = string.Empty;
}