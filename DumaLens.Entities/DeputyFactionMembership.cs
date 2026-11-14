using DumaLens.Entities.Contracts;

namespace DumaLens.Entities;

/// <summary>
/// Членство депутата во фракционном периоде
/// </summary>
/// <remarks>
/// Границы членства — по данным голосований; депутат может сменить фракцию внутри созыва.
/// </remarks>
public class DeputyFactionMembership : DbBaseEntity
{
    /// <summary>
    /// Идентификатор депутата
    /// </summary>
    public Guid DeputyId { get; set; }

    /// <summary>
    /// Идентификатор фракционного периода
    /// </summary>
    public Guid FactionPeriodId { get; set; }

    /// <summary>
    /// Дата начала членства (null, если не установлена)
    /// </summary>
    public DateOnly? DateFrom { get; set; }

    /// <summary>
    /// Дата окончания членства (null, если не установлена)
    /// </summary>
    public DateOnly? DateTo { get; set; }

    /// <summary>
    /// Депутат
    /// </summary>
    public Deputy Deputy { get; set; } = null!;

    /// <summary>
    /// Фракционный период
    /// </summary>
    public FactionPeriod FactionPeriod { get; set; } = null!;
}