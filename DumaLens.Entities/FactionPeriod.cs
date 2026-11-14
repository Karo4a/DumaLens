using DumaLens.Entities.Contracts;

namespace DumaLens.Entities;

/// <summary>
/// Исторический период фракции в созыве
/// </summary>
/// <remarks>
/// Site-id и название фракции меняются между созывами (СР: …25 → …27 → …29), поэтому период — отдельная сущность (ADR-8).
/// </remarks>
public class FactionPeriod : DbBaseEntity
{
    /// <summary>
    /// Идентификатор канонической фракции
    /// </summary>
    public Guid FactionId { get; set; }

    /// <summary>
    /// Идентификатор созыва
    /// </summary>
    public Guid ConvocationId { get; set; }

    /// <summary>
    /// Идентификатор фракции на сайте голосований (например, 72100024)
    /// </summary>
    public long SiteId { get; set; }

    /// <summary>
    /// Название фракции в этом созыве («ЕР», «СР-Р»)
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

    /// <summary>
    /// Каноническая фракция
    /// </summary>
    public Faction Faction { get; set; } = null!;

    /// <summary>
    /// Созыв
    /// </summary>
    public Convocation Convocation { get; set; } = null!;
}