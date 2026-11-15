namespace DumaLens.Api.Models.Factions;

/// <summary>
/// Каноническая фракция с историческими периодами
/// </summary>
public class FactionApiModel
{
    /// <summary>
    /// Код канонической фракции
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Название канонической фракции
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Исторические периоды фракции по созывам
    /// </summary>
    public IReadOnlyCollection<FactionPeriodApiModel> Periods { get; set; } = [];

    /// <summary>
    /// Сводная статистика по созывам (заполняется только в карточке фракции)
    /// </summary>
    public IReadOnlyCollection<FactionConvocationStatsApiModel> ConvocationStats { get; set; } = [];
}